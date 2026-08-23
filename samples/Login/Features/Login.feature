Feature: Login

  Scenario: Successful login
    Given the user is on the login page
    When the user logs in with valid credentials
    Then the dashboard should be displayed

  Scenario: Invalid login
    Given the user is on the login page
    When the user logs in with invalid credentials
    Then the login error should be displayed
