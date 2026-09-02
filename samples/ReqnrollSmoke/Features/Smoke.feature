Feature: Reqnroll Smoke

  Scenario: Deterministic local page flow
    Given the smoke page is open
    When the user submits the smoke form
    Then the success message should be visible
