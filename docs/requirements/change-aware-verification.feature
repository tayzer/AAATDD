@REQ_CHANGE_VERIFICATION
Feature: Change-aware verification
  TADA selects proportionate evidence and preserves the distinction between
  human-approved business acceptance and delegated engineering checks.

  @AC_MULTILEVEL
  Scenario: Verify a feature across several services
    Given an approved business baseline for a local service journey
    And a captured plan selecting service, contract and journey checks
    When all required checks execute with complete passing evidence
    Then the plan assessment is PASS
    And each check identifies its scope and retained evidence

  @AC_SUPPORTING_FAILURE
  Scenario: A supporting failure blocks completion
    Given a plan selecting business acceptance and supporting tests
    When business acceptance passes and a supporting test fails
    Then the plan assessment is FAIL
    And the supporting failure does not establish business RED

  @AC_COVERAGE_GAP
  Scenario: Missing required cases block completion
    Given a captured plan with required test identities
    When a required case is absent or skipped
    Then the plan assessment is INDETERMINATE

  @AC_APPROVAL_BOUNDARY
  Scenario: Engineering selection cannot approve business expectations
    Given a plan referencing an unapproved business baseline
    When the agent prepares or executes the plan
    Then no business approval is created
    And business acceptance cannot pass

  @AC_NO_TESTS
  Scenario: A documentation chore needs no test execution
    Given a documentation-only chore with an evaluable objective
    And a recorded reason for selecting no executable checks
    When the plan is assessed
    Then it reports that no checks were selected
    And it makes no passing test claim

  @AC_HISTORY
  Scenario: A passing rerun preserves an earlier failure
    Given a verified failure for the same check inputs and implementation
    When an unchanged rerun reports passing tests
    Then the assessment retains the unresolved earlier failure

  @AC_SERVICE_COVERAGE
  Scenario: Every required service needs runtime evidence
    Given a check requiring several named local services
    When a required service lacks complete runtime observations
    Then the check assessment is INDETERMINATE
