// TADA's loader callback only writes preallocated memory in this module.
// No CLR, allocation, file I/O, runtime library or imported function is called there.
// https://learn.microsoft.com/en-us/windows/win32/devnotes/ldrdllnotification
#include <stdint.h>
#include <stdatomic.h>

#define EXPORT __declspec(dllexport)
#define CAPACITY 4096
#define PATH_CHARACTERS 2048
_Static_assert(sizeof(void *) == 8, "The supported native observer is Windows x64.");
_Static_assert(ATOMIC_INT_LOCK_FREE == 2, "The loader callback requires inline lock-free atomics.");

__declspec(dllimport) void * __stdcall GetModuleHandleW(const uint16_t *name);
__declspec(dllimport) void * __stdcall GetProcAddress(void *module, const char *name);

typedef struct {
    uint16_t length;
    uint16_t maximum_length;
    const uint16_t *buffer;
} unicode_string;

typedef struct {
    uint32_t flags;
    const unicode_string *full_name;
    const unicode_string *base_name;
    void *base;
    uint32_t image_size;
} notification_data;
_Static_assert(sizeof(unicode_string) == 16 && sizeof(notification_data) == 40, "Unexpected Windows loader ABI layout.");

typedef void (__stdcall *notification_callback)(uint32_t, const notification_data *, void *);
typedef int32_t (__stdcall *register_notification)(uint32_t, notification_callback, void *, void **);
typedef int32_t (__stdcall *unregister_notification)(void *);

typedef struct {
    atomic_int ready;
    uint32_t length;
    volatile uint16_t path[PATH_CHARACTERS];
} load_event;

static load_event events[CAPACITY];
static atomic_int count;
static atomic_int failure;
static void *cookie;
static unregister_notification unregister_callback;

static void __stdcall on_notification(uint32_t reason, const notification_data *data, void *context)
{
    (void)context;
    if (reason != 1) return; // Load notifications become obligations, including short-lived images.
    int index = atomic_fetch_add_explicit(&count, 1, memory_order_relaxed);
    if (index < 0 || index >= CAPACITY) {
        atomic_store_explicit(&failure, 1, memory_order_release);
        return;
    }
    if (!data || !data->full_name || !data->full_name->buffer || (data->full_name->length & 1)) {
        atomic_store_explicit(&failure, 2, memory_order_release);
        return;
    }
    uint32_t length = data->full_name->length / 2;
    if (length == 0 || length >= PATH_CHARACTERS) {
        atomic_store_explicit(&failure, 3, memory_order_release);
        return;
    }
    events[index].length = length;
    for (uint32_t i = 0; i < length; ++i) events[index].path[i] = data->full_name->buffer[i];
    events[index].path[length] = 0;
    // Release/acquire publishes the complete path, never a partially copied notification.
    atomic_store_explicit(&events[index].ready, 1, memory_order_release);
}

EXPORT int TadaNativeProtocol(void) { return 1; }

EXPORT int32_t TadaNativeStart(void)
{
    if (cookie) return -2;
    int previous = atomic_load_explicit(&count, memory_order_relaxed);
    for (int i = 0; i < previous && i < CAPACITY; ++i) atomic_store_explicit(&events[i].ready, 0, memory_order_relaxed);
    atomic_store_explicit(&count, 0, memory_order_relaxed);
    atomic_store_explicit(&failure, 0, memory_order_relaxed);
    static const uint16_t ntdll[] = { 'n','t','d','l','l','.','d','l','l',0 };
    void *module = GetModuleHandleW(ntdll);
    if (!module) return -3;
    register_notification register_callback = (register_notification)GetProcAddress(module, "LdrRegisterDllNotification");
    unregister_callback = (unregister_notification)GetProcAddress(module, "LdrUnregisterDllNotification");
    if (!register_callback || !unregister_callback) return -4;
    return register_callback(0, on_notification, 0, &cookie);
}

EXPORT int32_t TadaNativeStop(void)
{
    if (!cookie || !unregister_callback) return -5;
    int32_t status = unregister_callback(cookie);
    if (status >= 0) cookie = 0;
    return status;
}

EXPORT int TadaNativeCount(void) { return atomic_load_explicit(&count, memory_order_acquire); }
EXPORT int TadaNativeStatus(void) { return atomic_load_explicit(&failure, memory_order_acquire); }

EXPORT int TadaNativeRead(uint32_t index, uint16_t *destination, uint32_t capacity)
{
    if (index >= CAPACITY || !destination) return -1;
    if (index >= (uint32_t)atomic_load_explicit(&count, memory_order_acquire) ||
        !atomic_load_explicit(&events[index].ready, memory_order_acquire)) return 0;
    uint32_t length = events[index].length;
    if (capacity <= length) return -2;
    for (uint32_t i = 0; i <= length; ++i) destination[i] = events[index].path[i];
    return 1;
}

// No CRT startup is linked. Windows zero-initialises the static queue before this entry point.
int __stdcall DllMain(void *module, uint32_t reason, void *reserved)
{
    (void)module; (void)reason; (void)reserved;
    return 1;
}
