# Submission Notes

This solution implements the ParaBank practical automation task as a C#/.NET 8 RPA-style web application.

## Scope covered

- Reads the customer input file uploaded at runtime.
- Registers each customer in ParaBank, or logs in when a username already exists.
- Opens a new bank account for each customer.
- Requests a 10,000 USD loan.
- Calculates the down payment as 20% of the customer's initial deposit, as specified in the task.
- Logs out after processing each customer and closes the browser at the end of the run.
- Generates an Excel operator report in USD and EUR, including DOB, debit card number, and CVV.

## Technical approach

The browser workflow is implemented with Selenium and split into dedicated flow classes for registration, login, account opening, and loan request. The orchestration layer depends on abstractions for input reading, validation, report writing, currency conversion, and RPA service creation. Unit tests cover parsing, validation, money conversion, and Excel report generation with synthetic test fixtures only.

## Observation / roadblock

ParaBank validates the loan down payment against the actual balance available in the ParaBank demo account. The supplied input file contains an initial deposit value, but the ParaBank UI does not provide a direct deposit step during registration/open-account flow. Therefore, a loan request may be rejected with an insufficient-funds message even when the automation correctly calculated the required down payment from the input file. The application captures this as a business result in the operator report.
