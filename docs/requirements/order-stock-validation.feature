# Business intent agreed 2026-10-27 in ADR 0006; executable-baseline approval pending.
# Requirement and scenario tags are proposed identities, not an agreed ID schema.
# Bindings do not exist. This file authorises no implementation or observer exception.

@REQ_DEMO_STOCK_001
Feature: Order stock validation
  For this first experiment, each order request has a final outcome
  before the customer receives its result.

  Rule: An order cannot consume more stock than is available

    @AC_DEMO_STOCK_001_01
    Scenario: Reject an order exceeding available stock
      Given product "ABC123" has 2 units available
      And the customer has no existing orders
      When the customer orders 3 units of "ABC123"
      Then the order is rejected
      And no order is created for that request
      And product "ABC123" still has 2 units available

    @AC_DEMO_STOCK_001_02
    Scenario: Accept an order using all available stock
      Given product "ABC123" has 2 units available
      And the customer has no existing orders
      When the customer orders 2 units of "ABC123"
      Then the order is accepted
      And exactly one order is created for 2 units of "ABC123"
      And product "ABC123" has 0 units available
