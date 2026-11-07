#include <stdint.h>
__declspec(dllexport) int TadaFixtureValue(void) { return 42; }
int __stdcall DllMain(void *module, uint32_t reason, void *reserved)
{
    (void)module; (void)reason; (void)reserved;
    return 1;
}
