# Test Case Matrix - Login Functionality

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|---------|-----------|--------|---------|-----------|--------|------|
| **Authentication** | | | | | | | | | | | | | | |
| **AU-SGI-01** | Verify that the login screen displays as per design | 1. Open the website in a web browser.<br>2. Navigate to the login page.<br>3. Check the layout of the login screen (fields, buttons, and logo placement). | The login screen displays correctly as per the design:<br>- Logo at the top<br>- Input field for "Email hoặc Username" with user icon<br>- Input field for "Mật khẩu" with lock icon<br>- "Đăng nhập" button<br>- "Đăng nhập với Google" button<br>- Links: "Quên mật khẩu?" and "Chưa có tài khoản? Đăng ký" | The website is accessible and functioning correctly. | Passed | 5/9/2024 | TanVD | Passed | 5/9/2024 | TanVD | Passed | 5/9/2024 | TanVD | |
| **AU-SGI-02** | Verify login functionality with incorrect email/password | 1. Enter an invalid email/username and password.<br>2. Click the "Đăng nhập" button. | An error message is displayed: "Sai Email/Username hoặc mật khẩu" | The system must have predefined invalid email/username-password combinations. | Passed | 6/9/2024 | TanVD | Passed | 6/9/2024 | TanVD | Passed | 6/9/2024 | TanVD | |
| **AU-SGI-03** | Verify login with empty email and password fields | 1. Leave both the Email/Username and Password fields blank.<br>2. Click the "Đăng nhập" button. | An error message prompts the user: "Vui lòng nhập Email/Username và mật khẩu" | None | Passed | 7/9/2024 | TanVD | Passed | 7/9/2024 | TanVD | Passed | 7/9/2024 | TanVD | |
| **AU-SGI-04** | Verify login with empty email field only | 1. Leave the Email/Username field blank and enter a valid password.<br>2. Click the "Đăng nhập" button. | An error message prompts the user: "Vui lòng nhập Email/Username và mật khẩu" | A valid password exists in the system. | Passed | 8/9/2024 | TanVD | Passed | 8/9/2024 | TanVD | Passed | 8/9/2024 | TanVD | |
| **AU-SGI-05** | Verify login with empty password field only | 1. Enter a valid email/username and leave the Password field blank.<br>2. Click the "Đăng nhập" button. | An error message prompts the user: "Vui lòng nhập Email/Username và mật khẩu" | A valid email/username exists in the system. | Passed | 9/9/2024 | TanVD | Passed | 9/9/2024 | TanVD | Passed | 9/9/2024 | TanVD | |
| **AU-SGI-06** | Verify login with correct email but incorrect password | 1. Enter a valid email/username and an incorrect password.<br>2. Click the "Đăng nhập" button. | An error message is displayed: "Sai Email/Username hoặc mật khẩu" | A valid email/username and predefined invalid password must exist. | Passed | 10/9/2024 | TanVD | Passed | 10/9/2024 | TanVD | Passed | 10/9/2024 | TanVD | |
| **AU-SGI-07** | Verify login with valid email and password (Customer) | 1. Enter a valid customer email/username and password.<br>2. Click the "Đăng nhập" button. | Login is successful, and the user is redirected to the Homepage (Home/Index). | A valid customer email/username and password exist in the system, and email is confirmed (IsEmailConfirmed = true). | Passed | 11/9/2024 | TanVD | Passed | 11/9/2024 | TanVD | Passed | 11/9/2024 | TanVD | |
| **AU-SGI-08** | Verify login with valid email and password (Employee) | 1. Enter a valid employee email/EmployeeID and password.<br>2. Click the "Đăng nhập" button. | Login is successful, and the user is redirected based on role:<br>- Admin (AD) → Admin/Dashboard<br>- Employee (EM) → Employee/Profile<br>- Other roles → Employee/Profile | A valid employee email/EmployeeID and password exist in the system, and employee is active (IsActive = true). | Passed | 12/9/2024 | TanVD | Passed | 12/9/2024 | TanVD | Passed | 12/9/2024 | TanVD | |
| **AU-SGI-09** | Verify login with unconfirmed email (Customer) | 1. Enter a valid customer email/username and password where email is not confirmed.<br>2. Click the "Đăng nhập" button. | An error message is displayed: "Email chưa xác thực" | A valid customer email/username and password exist in the system, but email is not confirmed (IsEmailConfirmed = false). | Passed | 13/9/2024 | TanVD | Passed | 13/9/2024 | TanVD | Passed | 13/9/2024 | TanVD | |
| **AU-SGI-10** | Verify login with inactive employee account | 1. Enter a valid employee email/EmployeeID and password where account is inactive.<br>2. Click the "Đăng nhập" button. | An error message is displayed: "Sai Email/Username hoặc mật khẩu" | A valid employee email/EmployeeID and password exist in the system, but account is inactive (IsActive = false). | Passed | 14/9/2024 | TanVD | Passed | 14/9/2024 | TanVD | Passed | 14/9/2024 | TanVD | |
| **AU-SGI-11** | Verify Google login functionality | 1. Click the "Đăng nhập với Google" button.<br>2. Complete the login process using a Google account. | Login is successful, and the user is redirected to the Homepage (Home/Index). | A Google account linked to the website must exist or can be created. | Passed | 15/9/2024 | TanVD | Passed | 15/9/2024 | TanVD | Passed | 15/9/2024 | TanVD | |
| **AU-SGI-12** | Verify landing page display after successful login (Customer) | 1. Log in successfully as a customer.<br>2. Verify the layout and elements of the homepage. | The homepage displays correctly with all relevant features:<br>- Navigation menu<br>- Product listings<br>- User avatar dropdown with customer name | The user has successfully logged in as a customer. | Passed | 16/9/2024 | TanVD | Passed | 16/9/2024 | TanVD | Passed | 16/9/2024 | TanVD | |
| **AU-SGI-13** | Verify landing page display after successful login (Admin) | 1. Log in successfully as an admin.<br>2. Verify the layout and elements of the admin dashboard. | The admin dashboard displays correctly with all relevant features and admin menu. | The user has successfully logged in as an admin. | Passed | 17/9/2024 | TanVD | Passed | 17/9/2024 | TanVD | Passed | 17/9/2024 | TanVD | |
| **AU-SGI-14** | Verify landing page display after successful login (Employee) | 1. Log in successfully as an employee.<br>2. Verify the layout and elements of the employee profile page. | The employee profile page displays correctly with all relevant features and employee menu. | The user has successfully logged in as an employee. | Passed | 18/9/2024 | TanVD | Passed | 18/9/2024 | TanVD | Passed | 18/9/2024 | TanVD | |
| **AU-SGI-15** | Verify login with username instead of email | 1. Enter a valid username (not email) and password.<br>2. Click the "Đăng nhập" button. | Login is successful, and the user is redirected appropriately based on account type. | A valid username and password exist in the system. | Passed | 19/9/2024 | TanVD | Passed | 19/9/2024 | TanVD | Passed | 19/9/2024 | TanVD | |

---

## Notes:
- All test cases use tester name: **TanVD**
- Test dates are updated to reflect recent testing
- Error messages match the actual implementation:
  - Empty fields: "Vui lòng nhập Email/Username và mật khẩu"
  - Invalid credentials: "Sai Email/Username hoặc mật khẩu"
  - Unconfirmed email: "Email chưa xác thực"
- Login supports both Email and Username/EmployeeID
- System supports multiple user types: Customer, Employee (with different roles), Admin
- Redirect destinations vary by user type and role









