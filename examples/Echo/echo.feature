# Demonstration candidate. Automated controls do not substitute for human business approval.
@REQ_ECHO_MESSAGE
Feature: Echo a message
  @AC_ECHO_SPACES
  Scenario: Preserve the submitted message
    Given an isolated echo API
    When I send the message "hello world"
    Then the echo status is 200
    And the echoed message is "hello world"

  @AC_ECHO_EMPTY
  Scenario: Reject an empty message
    Given an isolated echo API
    When I send the message ""
    Then the echo status is 400
    And the error is "message_required"
