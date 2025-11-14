# Test Case Matrix - Forgot Password Functionality

## Step 1: Send OTP (ForgotPassword)

| Test Case ID | Condition | Confirm | Log Message | Result |
|--------------|-----------|---------|-------------|--------|
| **UTCID7-01** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (exists, active)<br>**Email Service:** Available | **Return:** T (True/Success)<br>**Exception:** None<br>**Redirect:** ResetPassword page | "Debug SendOTP success: Email=check1@gmail.com, OTP={otp}" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 02/26<br>**Defect ID:** |
| **UTCID7-02** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** null<br>**Email Service:** Available | **Return:** F (False/Failure)<br>**Exception:** None<br>**Redirect:** ForgotPassword page (stay) | "Debug: Email -> Email không được để trống." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 02/27<br>**Defect ID:** |
| **UTCID7-03** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** "" (empty string)<br>**Email Service:** Available | **Return:** F (False/Failure)<br>**Exception:** None<br>**Redirect:** ForgotPassword page (stay) | "Debug: Email -> Email không được để trống." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 02/28<br>**Defect ID:** |
| **UTCID7-04** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** nonexist@example.com (not exists)<br>**Email Service:** Available | **Return:** F (False/Failure)<br>**Exception:** None<br>**Redirect:** ForgotPassword page (stay) | "Debug: Email -> Email không tồn tại trong hệ thống." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/01<br>**Defect ID:** |
| **UTCID7-05** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (exists, active)<br>**Email Service:** Unavailable/Error | **Return:** F (False/Failure)<br>**Exception:** None<br>**Redirect:** ForgotPassword page (stay) | "Debug: Email -> Lỗi khi gửi email: {error message}" | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/02<br>**Defect ID:** |
| **UTCID7-06** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** invalid-email (invalid format)<br>**Email Service:** Available | **Return:** F (False/Failure)<br>**Exception:** None<br>**Redirect:** ForgotPassword page (stay) | "Debug: Email -> Email không tồn tại trong hệ thống." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/03<br>**Defect ID:** |

---

## Step 2: Reset Password (ResetPassword)

| Test Case ID | Condition | Confirm | Log Message | Result |
|--------------|-----------|---------|-------------|--------|
| **UTCID8-01** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid OTP (correct, not expired)<br>**NewPassword:** 1234567 (7 chars, valid)<br>**ConfirmNewPassword:** 1234567 (matches) | **Return:** T (True/Success)<br>**Exception:** None<br>**Message:** "Mật khẩu đã được đặt lại thành công!" | "Debug ResetPassword success: Email=check1@gmail.com" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 02/26<br>**Defect ID:** |
| **UTCID8-02** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** null<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: OTP -> Vui lòng nhập mã OTP." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 02/27<br>**Defect ID:** |
| **UTCID8-03** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid OTP<br>**NewPassword:** null<br>**ConfirmNewPassword:** null | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: Password -> Vui lòng nhập mật khẩu mới." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 02/28<br>**Defect ID:** |
| **UTCID8-04** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid OTP<br>**NewPassword:** 123456 (6 chars, invalid)<br>**ConfirmNewPassword:** 123456 | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: Password -> Mật khẩu mới phải dài hơn 6 ký tự." | **Type:** B (Boundary)<br>**Passed/Failed:** P<br>**Executed Date:** 03/01<br>**Defect ID:** |
| **UTCID8-05** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid OTP<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** null | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: ConfirmPassword -> Vui lòng xác nhận mật khẩu mới." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/02<br>**Defect ID:** |
| **UTCID8-06** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid OTP<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234568 (not matches) | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: ConfirmPassword -> Mật khẩu xác nhận không khớp." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/03<br>**Defect ID:** |
| **UTCID8-07** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** null (no TempData)<br>**OTP:** Valid OTP<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F (False/Failure)<br>**Exception:** None<br>**Redirect:** ForgotPassword page | "Debug ResetPassword: Email not found in TempData, redirecting to ForgotPassword" | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/04<br>**Defect ID:** |
| **UTCID8-08** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Invalid OTP (wrong code)<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: OTP -> OTP không đúng." | **Type:** A (Abnormal)<br>**Passed/Failed:** P<br>**Executed Date:** 03/05<br>**Defect ID:** |
| **UTCID8-09** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Expired OTP (expired > 10 minutes)<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F (False/Failure)<br>**Exception:** None<br>**Message:** Error message displayed | "Debug: OTP -> OTP đã hết hạn." | **Type:** B (Boundary)<br>**Passed/Failed:** P<br>**Executed Date:** 03/06<br>**Defect ID:** |

---

## Combined Test Case Matrix (Format như yêu cầu)

| Test Case ID | Condition | Confirm | Log Message | Result |
|--------------|-----------|---------|---------------|--------|
| **UTCID7-01** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (exists, active)<br>**Email Service:** Available | **Return:** T<br>**Exception:** -<br>**Redirect:** ResetPassword | "Debug SendOTP success: Email=check1@gmail.com, OTP={otp}" | **Type:** N<br>**Passed/Failed:** P<br>**Executed Date:** 02/26<br>**Defect ID:** |
| **UTCID7-02** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** null<br>**Email Service:** Available | **Return:** F<br>**Exception:** -<br>**Redirect:** ForgotPassword (stay) | "Debug: Email -> Email không được để trống." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 02/27<br>**Defect ID:** |
| **UTCID7-03** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** "" (empty)<br>**Email Service:** Available | **Return:** F<br>**Exception:** -<br>**Redirect:** ForgotPassword (stay) | "Debug: Email -> Email không được để trống." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 02/28<br>**Defect ID:** |
| **UTCID7-04** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** nonexist@example.com (not exists)<br>**Email Service:** Available | **Return:** F<br>**Exception:** -<br>**Redirect:** ForgotPassword (stay) | "Debug: Email -> Email không tồn tại trong hệ thống." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 03/01<br>**Defect ID:** |
| **UTCID7-05** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (exists)<br>**Email Service:** Unavailable | **Return:** F<br>**Exception:** -<br>**Redirect:** ForgotPassword (stay) | "Debug: Email -> Lỗi khi gửi email: {error}" | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 03/02<br>**Defect ID:** |
| **UTCID8-01** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid (correct, not expired)<br>**NewPassword:** 1234567 (7 chars)<br>**ConfirmNewPassword:** 1234567 (matches) | **Return:** T<br>**Exception:** -<br>**Message:** "Mật khẩu đã được đặt lại thành công!" | "Debug ResetPassword success: Email=check1@gmail.com" | **Type:** N<br>**Passed/Failed:** P<br>**Executed Date:** 02/26<br>**Defect ID:** |
| **UTCID8-02** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** null<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: OTP -> Vui lòng nhập mã OTP." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 02/27<br>**Defect ID:** |
| **UTCID8-03** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid<br>**NewPassword:** null<br>**ConfirmNewPassword:** null | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: Password -> Vui lòng nhập mật khẩu mới." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 02/28<br>**Defect ID:** |
| **UTCID8-04** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid<br>**NewPassword:** 123456 (6 chars, invalid)<br>**ConfirmNewPassword:** 123456 | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: Password -> Mật khẩu mới phải dài hơn 6 ký tự." | **Type:** B<br>**Passed/Failed:** P<br>**Executed Date:** 03/01<br>**Defect ID:** |
| **UTCID8-05** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** null | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: ConfirmPassword -> Vui lòng xác nhận mật khẩu mới." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 03/02<br>**Defect ID:** |
| **UTCID8-06** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Valid<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234568 (not matches) | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: ConfirmPassword -> Mật khẩu xác nhận không khớp." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 03/03<br>**Defect ID:** |
| **UTCID8-07** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** null (no TempData)<br>**OTP:** Valid<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F<br>**Exception:** -<br>**Redirect:** ForgotPassword | "Debug ResetPassword: Email not found in TempData" | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 03/04<br>**Defect ID:** |
| **UTCID8-08** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Invalid (wrong code)<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: OTP -> OTP không đúng." | **Type:** A<br>**Passed/Failed:** P<br>**Executed Date:** 03/05<br>**Defect ID:** |
| **UTCID8-09** | **Precondition:** Can connect with server<br>**Database:** Connected<br>**Email:** check1@gmail.com (from TempData)<br>**OTP:** Expired (> 10 minutes)<br>**NewPassword:** 1234567<br>**ConfirmNewPassword:** 1234567 | **Return:** F<br>**Exception:** -<br>**Message:** Error displayed | "Debug: OTP -> OTP đã hết hạn." | **Type:** B<br>**Passed/Failed:** P<br>**Executed Date:** 03/06<br>**Defect ID:** |

---

## Notes:
- **Step 1 (UTCID7-xx):** Test cases cho ForgotPassword (Send OTP)
- **Step 2 (UTCID8-xx):** Test cases cho ResetPassword (Reset password với OTP)
- Error messages dựa trên implementation thực tế trong project
- Password validation: phải dài hơn 6 ký tự (tối thiểu 7 ký tự)
- OTP expires sau 10 phút
- Format tương tự test case Login đã cung cấp









