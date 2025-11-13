# Test Case Matrix - UC Logout

## Test Case Format (tương tự Register)

| Test Case ID | Condition | Confirm | Log Message | Result |
|--------------|-----------|---------|-------------|--------|
| **UTCID6-01** | **Precondition:** Can connect with server<br>**Input:** User authenticated as Customer (CustomerScheme)<br>**Session Keys:** CustomerID=1, CustomerName="Nguyen Van A", Role="Customer" | **Return:** T (True/Success)<br>**Exception:** None<br>**Redirect:** Home/Index | "Debug Logout success: UserType=Customer, CustomerID=1" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |
| **UTCID6-02** | **Precondition:** Can connect with server<br>**Input:** User authenticated as Admin (AdminScheme)<br>**Session Keys:** EmployeeID="EMP001", EmployeeName="Admin User", Role="AD", RoleID="AD", BranchId=1 | **Return:** T (True/Success)<br>**Exception:** None<br>**Redirect:** Home/Index | "Debug Logout success: UserType=Admin, EmployeeID=EMP001" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |
| **UTCID6-03** | **Precondition:** Can connect with server<br>**Input:** User authenticated as Employee (EmployeeScheme)<br>**Session Keys:** EmployeeID="EMP002", EmployeeName="Employee User", Role="EM", RoleID="EM", BranchId=1 | **Return:** T (True/Success)<br>**Exception:** None<br>**Redirect:** Home/Index | "Debug Logout success: UserType=Employee, EmployeeID=EMP002" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |
| **UTCID6-04** | **Precondition:** Can connect with server<br>**Input:** User NOT authenticated (no scheme)<br>**Session Keys:** None | **Return:** T (True/Success)<br>**Exception:** None<br>**Redirect:** Home/Index | "Debug Logout: User not authenticated, redirecting to Home" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |
| **UTCID6-05** | **Precondition:** Can connect with server<br>**Input:** User authenticated as Customer<br>**Session Keys:** CustomerID=1, CustomerName="Nguyen Van A", Role="Customer"<br>**Action:** Verify session cleared after logout | **Return:** T (True/Success)<br>**Exception:** None<br>**Session After Logout:** All session keys removed | "Debug Logout success: UserType=Customer, CustomerID=1"<br>"Debug: Session cleared - CustomerID, CustomerName, Role removed" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |
| **UTCID6-06** | **Precondition:** Can connect with server<br>**Input:** User authenticated as Admin<br>**Session Keys:** EmployeeID="EMP001", EmployeeName="Admin User", Role="AD", RoleID="AD", BranchId=1<br>**Action:** Verify session cleared after logout | **Return:** T (True/Success)<br>**Exception:** None<br>**Session After Logout:** All session keys removed | "Debug Logout success: UserType=Admin, EmployeeID=EMP001"<br>"Debug: Session cleared - EmployeeID, EmployeeName, Role, RoleID, BranchId removed" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |
| **UTCID6-07** | **Precondition:** Can connect with server<br>**Input:** User authenticated as Employee<br>**Session Keys:** EmployeeID="EMP002", EmployeeName="Employee User", Role="EM", RoleID="EM", BranchId=1<br>**Action:** Verify session cleared after logout | **Return:** T (True/Success)<br>**Exception:** None<br>**Session After Logout:** All session keys removed | "Debug Logout success: UserType=Employee, EmployeeID=EMP002"<br>"Debug: Session cleared - EmployeeID, EmployeeName, Role, RoleID, BranchId removed" | **Type:** N (Normal)<br>**Passed/Failed:** P<br>**Executed Date:** 1<br>**Defect ID:** |

---

## Chi tiết Test Cases

### UTCID6-01: Logout Customer (Success)
- **Precondition:** Server connected, User đã login với CustomerScheme
- **Input:** 
  - Authentication: CustomerScheme
  - Session: CustomerID=1, CustomerName="Nguyen Van A", Role="Customer"
- **Expected:**
  - Return: True
  - Session keys removed: CustomerID, CustomerName, Role
  - SignOut: CustomerScheme
  - Redirect: Home/Index
- **Log Message:** "Debug Logout success: UserType=Customer, CustomerID=1"

### UTCID6-02: Logout Admin (Success)
- **Precondition:** Server connected, User đã login với AdminScheme
- **Input:**
  - Authentication: AdminScheme
  - Session: EmployeeID="EMP001", EmployeeName="Admin User", Role="AD", RoleID="AD", BranchId=1
- **Expected:**
  - Return: True
  - Session keys removed: EmployeeID, EmployeeName, Role, RoleID, BranchId
  - SignOut: AdminScheme
  - Redirect: Home/Index
- **Log Message:** "Debug Logout success: UserType=Admin, EmployeeID=EMP001"

### UTCID6-03: Logout Employee (Success)
- **Precondition:** Server connected, User đã login với EmployeeScheme
- **Input:**
  - Authentication: EmployeeScheme
  - Session: EmployeeID="EMP002", EmployeeName="Employee User", Role="EM", RoleID="EM", BranchId=1
- **Expected:**
  - Return: True
  - Session keys removed: EmployeeID, EmployeeName, Role, RoleID, BranchId
  - SignOut: EmployeeScheme
  - Redirect: Home/Index
- **Log Message:** "Debug Logout success: UserType=Employee, EmployeeID=EMP002"

### UTCID6-04: Logout khi chưa đăng nhập
- **Precondition:** Server connected, User chưa login
- **Input:**
  - Authentication: None
  - Session: None
- **Expected:**
  - Return: True
  - No SignOut (no scheme to sign out)
  - Redirect: Home/Index
- **Log Message:** "Debug Logout: User not authenticated, redirecting to Home"

### UTCID6-05: Verify Session Cleared - Customer
- **Precondition:** Server connected, User đã login với CustomerScheme
- **Input:**
  - Authentication: CustomerScheme
  - Session: CustomerID=1, CustomerName="Nguyen Van A", Role="Customer"
- **Expected:**
  - After logout, verify session keys are null/empty
  - CustomerID = null
  - CustomerName = null
  - Role = null
- **Log Message:** 
  - "Debug Logout success: UserType=Customer, CustomerID=1"
  - "Debug: Session cleared - CustomerID, CustomerName, Role removed"

### UTCID6-06: Verify Session Cleared - Admin
- **Precondition:** Server connected, User đã login với AdminScheme
- **Input:**
  - Authentication: AdminScheme
  - Session: EmployeeID="EMP001", EmployeeName="Admin User", Role="AD", RoleID="AD", BranchId=1
- **Expected:**
  - After logout, verify session keys are null/empty
  - EmployeeID = null
  - EmployeeName = null
  - Role = null
  - RoleID = null
  - BranchId = null
- **Log Message:**
  - "Debug Logout success: UserType=Admin, EmployeeID=EMP001"
  - "Debug: Session cleared - EmployeeID, EmployeeName, Role, RoleID, BranchId removed"

### UTCID6-07: Verify Session Cleared - Employee
- **Precondition:** Server connected, User đã login với EmployeeScheme
- **Input:**
  - Authentication: EmployeeScheme
  - Session: EmployeeID="EMP002", EmployeeName="Employee User", Role="EM", RoleID="EM", BranchId=1
- **Expected:**
  - After logout, verify session keys are null/empty
  - EmployeeID = null
  - EmployeeName = null
  - Role = null
  - RoleID = null
  - BranchId = null
- **Log Message:**
  - "Debug Logout success: UserType=Employee, EmployeeID=EMP002"
  - "Debug: Session cleared - EmployeeID, EmployeeName, Role, RoleID, BranchId removed"

---

## So sánh với Implementation hiện tại

### ✅ Đã có:
1. ✅ Logout method xử lý 3 schemes: AdminScheme, EmployeeScheme, CustomerScheme
2. ✅ Clear session keys tương ứng với từng scheme
3. ✅ SignOut authentication scheme
4. ✅ Redirect về Home/Index

### ❌ Thiếu:
1. ❌ **Debug log messages** - Không có log như test case yêu cầu
2. ❌ **Verification** - Không có test verify session đã được clear

---

## Notes:
- Format test case tương tự Register form (UTCID5-xx)
- Bao gồm cả Normal và Abnormal cases
- Có log messages để debug
- Verify session cleared sau logout







