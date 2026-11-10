@REQ_ORDER_JOURNEY
Feature: Asynchronous order journey
  The order, payment and inventory services coordinate a purchase.
  Each scenario owns one item and starts with no orders or payments.

  @AC_ORDER_COMPLETES
  Scenario: Approved payment completes the order
    Given the three services are ready with one available item
    When I submit an order with an approved payment
    Then the order becomes completed within 10 seconds
    And zero items are available
    And one charge exists

  @AC_PAYMENT_COMPENSATES
  Scenario: Declined payment releases the reserved item
    Given the three services are ready with one available item
    When I submit an order with a declined payment
    Then the order becomes rejected within 10 seconds
    And one item is available
    And no charges exist

  @AC_DUPLICATE_DELIVERY
  Scenario: Repeated order and outcome delivery preserve the result
    Given the three services are ready with one available item
    When I submit the same approved order twice
    Then the order becomes completed within 10 seconds
    When I deliver the recorded payment outcome again
    Then zero items are available
    And one charge exists
