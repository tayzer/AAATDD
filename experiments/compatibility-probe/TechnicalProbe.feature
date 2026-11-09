@PROBE_ONLY
Feature: Technical compatibility probe
  These synthetic cases test tooling, not Orders acceptance.

  @PROBE_CONTEXT
  Scenario: Observe isolated execution context
    Given an independent probe context
    When a technical value is recorded
    Then the technical value is observed

  @PROBE_SQLITE
  Scenario: Exercise SQLite connection roles
    Given an independent probe context
    When SQLite connection roles are exercised
    Then provider capabilities and cleanup are recorded
