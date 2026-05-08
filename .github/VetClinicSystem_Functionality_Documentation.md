# VetClinicSystem Functionality Documentation

## 1. Website Overview

| Item | Details |
|---|---|
| Project Name | VetClinicSystem |
| System Type | Veterinary clinic management website |
| Main Purpose | To manage clinic users, pets, surgery appointments, vaccination records, medical records, clinic information, and notifications |
| Main Users | Client, Staff, Admin |
| Core Workflow | Clients or guests request surgery appointments, clinic staff/admin review and finalize schedules, records and notifications are updated accordingly |

### Brief Description
VetClinicSystem is a role-based veterinary clinic website built to support clinic operations and client-facing pet management. It allows clients to register, manage their pets, request surgery appointments, view records, and receive notifications. Staff and admin users can manage appointments, pets, records, vaccinations, clinic information, and notifications. Admin users also manage user accounts and monitor overall system activity through the dashboard.

### Main System Focus
- Surgery appointment request and approval workflow
- Pet record management
- Vaccination and medical history tracking
- User account and role handling
- Notification updates for clinic and client activity

---

## 2. User Roles

### 2.1 Client

| Access Area | What Client Can Do |
|---|---|
| Account | Register, log in, log out, update profile, change password, deactivate own account |
| Dashboard | View personal dashboard with reminders, follow-ups, overdue vaccines, recent records, and calendar |
| Pets | Create, view, edit, delete only their own pets |
| Appointments | Create surgery requests, view own appointments, edit/cancel own appointments only when allowed |
| Notifications | View own notifications and mark them as read |
| Medical Records | View records tied to their own pets; current code also allows create/edit/delete for owned pets |
| Vaccinations | View records tied to their own pets; current code also allows create/edit/delete for owned pets |
| Clinic | View clinic information |

### 2.2 Staff

| Access Area | What Staff Can Do |
|---|---|
| Dashboard | View total appointments and unread notification count |
| Pets | View/search/manage pet records |
| Appointments | View all, filter, approve, reject, reschedule, edit, complete, cancel |
| Medical Records | Create, edit, view, delete records |
| Vaccinations | Create, edit, view, delete records |
| Services | Manage services |
| Clinic | View clinic information |
| Notifications | View staff notifications and mark them as read |

### 2.3 Admin

| Access Area | What Admin Can Do |
|---|---|
| Dashboard | View total pets, appointments, users, unread notifications, and chart data |
| Users | View all users, deactivate accounts, restore accounts |
| Pets | Same operational access as staff |
| Appointments | Same operational access as staff |
| Medical Records | Same operational access as staff |
| Vaccinations | Same operational access as staff |
| Services | Same operational access as staff |
| Clinic | View and edit clinic information |
| Notifications | View staff notifications and mark them as read |

---

## 3. System Architecture Summary

| Layer | Purpose |
|---|---|
| Controllers | Handle HTTP requests, role checks, validation flow, and page actions |
| Services | Contain business logic and workflow behavior |
| Repositories | Perform database read/write operations |
| Models | Represent database tables and request models |
| Views | User interface pages |
| Program.cs | Configures services, session, database connection, and startup schema updates |

---

## 4. Controllers and Main Responsibilities

| Controller | Main Responsibilities |
|---|---|
| `AccountController` | Login, register, account settings, password change, deactivate account, logout |
| `AppointmentsController` | Appointment list, create, guest create, edit, delete/cancel, details, approve, reject, complete |
| `ClinicController` | View clinic information, admin edit clinic info |
| `DashboardController` | Admin dashboard, staff dashboard, client dashboard, admin users page, deactivate/restore users |
| `HomeController` | Public home page |
| `MedicalRecordsController` | Medical record CRUD with ownership filtering |
| `NotificationsController` | Staff/admin notifications, client notifications, mark-as-read |
| `PetsController` | Pet CRUD with ownership filtering |
| `ServicesController` | Service CRUD |
| `VaccinationsController` | Vaccination record CRUD with ownership filtering |

---

## 5. Complete Feature List

### 5.1 Public Website Features

| Feature Name | Who Can Use It | View/Page | Controller/Action | What It Does | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| Landing Page | Public | `Views/Home/Index.cshtml` | `Home/Index` | Shows hero section, services, prices, articles, contacts, request form | Mostly static content | None | Open homepage and verify all sections load |
| Service Carousel | Public | Home page | Client-side JS | Rotates service cards | Static image/text content | None | Click left/right arrows |
| Price Tabs | Public | Home page | Client-side JS | Switches displayed service pricing info | Static price data in view script | None | Switch among all tabs |
| Useful Articles | Public | Home page | Client-side JS | Opens external pet-care article links by category | Hard-coded article links | None | Click each `Read Article` button |
| Contact Section | Public | Home page | N/A | Shows address, phone, email, schedule, map links | Static content | None | Click Google Maps and Facebook buttons |
| Leave a Request Form | Public | Home page | Client-side JS + `Appointments/GuestCreate` redirect | Allows selecting a service and continuing only if surgery is chosen | Form values passed as query string | Walk-in services do not continue to appointment form | Choose surgery vs walk-in services and compare behavior |

### 5.2 Authentication and Account Features

| Feature Name | Who Can Use It | View/Page | Controller/Action | What It Does | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| Login | Registered users | `Views/Account/Login.cshtml` | `Account/Login` | Authenticates and redirects by role | `User` | Inactive accounts blocked | Test client/staff/admin login |
| Register | Public | `Views/Account/Register.cshtml` | `Account/Register` | Creates client account and linked pet owner | `User`, `PetOwner`, `Role` | Username/email unique, valid PH number | Create a new client account |
| Logout | Logged-in users | Navbar | `Account/Logout` | Clears session | Session | None | Log out and verify redirect |
| Account Settings | Logged-in users | `Views/Account/Settings.cshtml` | `Account/Settings` | Update username, email, profile details, password | `User`, `PetOwner` | Gmail-only email, username rules, strong password rules | Edit fields and save |
| Deactivate Own Account | Logged-in users | Settings page | `Account/Deactivate` | Soft-deactivates own account | `User` | Must confirm username, password, and `DEACTIVATE` text | Try wrong values then correct values |

---

## 6. Dashboard Features

### 6.1 Client Dashboard

| Dashboard Feature | Description | Data Used | How to Test |
|---|---|---|---|
| Greeting + date | Shows client greeting and today’s date | `User`, `PetOwner` | Log in as client |
| Pet count | Shows total pets owned by client | `Pet` | Add/remove pets and refresh |
| Appointment count | Shows total client appointments | `Appointment` | Create appointments and refresh |
| Calendar | Shows appointment days and click-to-view popover | Serialized client appointment data | Click highlighted dates |
| Notifications shortcut | Shows unread client notifications count | `StaffNotification` | Trigger notifications and open dashboard |
| Appointment reminders | Shows appointments within next 2 days | `Appointment` | Create upcoming appointments |
| Follow-up reminders | Shows completed appointments | `Appointment` | Mark an appointment completed |
| Overdue vaccinations | Shows vaccines with past due dates | `VaccinationRecord` | Create past-due vaccine record |
| Recent treatment history | Shows recent medical records | `MedicalRecord` | Add medical records |

### 6.2 Staff Dashboard

| Dashboard Feature | Description | Data Used | How to Test |
|---|---|---|---|
| Total appointments | Shows appointment count | `Appointment` | Open staff dashboard |
| Unread notifications | Shows unread staff notifications count | `StaffNotification` | Trigger surgery request/update |

### 6.3 Admin Dashboard

| Dashboard Feature | Description | Data Used | How to Test |
|---|---|---|---|
| Total pets | Shows overall pet count | `Pet` | Open admin dashboard |
| Total appointments | Shows overall appointment count | `Appointment` | Open admin dashboard |
| Total users | Shows all users | `User` | Register new account and refresh |
| Unread notifications | Shows staff unread notifications count | `StaffNotification` | Trigger notifications |
| Appointments per day chart | Displays line chart of appointment volume | `Appointment` | Add appointments on multiple dates |

---

## 7. Pet Management

| Feature Name | Who Can Use It | View/Page | Controller/Action | What It Does | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| View Pets | Client, Staff, Admin | `Views/Pets/Index.cshtml` | `Pets/Index` | Lists pets | `Pet` | Clients see only own pets | Compare client vs staff/admin views |
| Search Pets | Client, Staff, Admin | Pets index | `Pets/Index(search)` | Searches by pet name/species/breed | `Pet` | Client search limited to owned pets | Search pet name/species |
| Add Pet | Client, Staff, Admin | `Views/Pets/Create.cshtml` | `Pets/Create` | Creates pet record | `Pet`, `PetOwner` | Client pet is auto-linked to owner profile | Add a pet while logged in as client |
| Edit Pet | Client, Staff, Admin | `Views/Pets/Edit.cshtml` | `Pets/Edit` | Updates pet data | `Pet` | Clients can edit only own pets | Edit a pet |
| Pet Details | Client, Staff, Admin | `Views/Pets/Details.cshtml` | `Pets/Details` | Shows pet information | `Pet` | Clients limited to own pets | Open details |
| Delete Pet | Client, Staff, Admin | `Views/Pets/Delete.cshtml` | `Pets/Delete` | Deletes pet and linked operational data | `Pet`, `Appointment`, `MedicalRecord`, `VaccinationRecord`, `StaffNotification`, `ReminderLog` | Clients limited to own pets | Delete a pet with related data |

### Pet Delete Behavior
When a pet is deleted:
- linked appointments are removed
- linked medical records are removed
- linked vaccination records are removed
- linked reminder logs are removed
- linked notifications have `AppointmentId` cleared first

---

## 8. Appointment Functionality

### 8.1 Appointment System Overview

| Item | Current Behavior |
|---|---|
| Appointment-based service | Surgery only |
| Other services | Walk-in only |
| Booking types | Logged-in client booking and guest booking |
| Final schedule control | Admin/staff finalizes actual surgery schedule |
| Status workflow | Pending -> Approved / Rejected / Completed |

### 8.2 Appointment List and Filtering

| Feature | Who Can Use It | View/Page | Controller/Action | Description | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|
| View appointment list | Client, Staff, Admin | `Views/Appointments/Index.cshtml` | `Appointments/Index` | Lists appointments with actions | Clients see only own appointments | Open appointment list as each role |
| Search appointments | Client, Staff, Admin | Appointment list | `Appointments/Index(search)` | Search by pet, service, or reason | Client results limited to own data | Search by pet name or reason |
| Filter by status | Client, Staff, Admin | Appointment list | `Appointments/Index(statusId)` | Filters by Pending/Approved/Rejected/Completed | None beyond ownership | Use each status filter |
| Filter by date | Client, Staff, Admin | Appointment list | `Appointments/Index(appointmentDate)` | Filters by exact date | None beyond ownership | Pick a date with known appointment |

### 8.3 Client Surgery Request Booking

| Feature | Who Can Use It | View/Page | Controller/Action | Description | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| Request Surgery Appointment | Logged-in clients | `Views/Appointments/Create.cshtml` | `Appointments/Create` | Submits surgery request with preferred date/time/category/emergency/notes | `Appointment`, `Pet`, `Service` | Must own selected pet; only surgery services allowed | Submit a surgery request |
| Preferred schedule capture | Logged-in clients | Create page | `Appointments/Create` | Stores requested date/time as preferred schedule | `PreferredAppointmentDate`, `PreferredAppointmentTime` | None beyond surgery rules | Submit request and inspect saved record |
| Emergency request flag | Logged-in clients | Create page | `Appointments/Create` | Marks request as emergency priority | `Appointment.IsEmergency` | Only affects capacity check and notifications | Toggle emergency and submit |

### 8.4 Guest Surgery Request Booking

| Feature | Who Can Use It | View/Page | Controller/Action | Description | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| Guest surgery booking form | Public | `Views/Appointments/GuestCreate.cshtml` | `Appointments/GuestCreate` | Allows non-logged-in user to request surgery | `GuestAppointmentBooking`, `User`, `PetOwner`, `Pet`, `Appointment` | Existing non-guest email cannot be reused | Submit guest booking |
| Guest account generation | Public | GuestCreate submit | `CreateGuestBooking()` | Creates guest user if needed | `User.IsGuest` | Username auto-generated | Inspect created guest user in DB/admin users |
| Guest pet creation | Public | GuestCreate submit | `CreateGuestBooking()` | Creates pet for guest owner | `Pet` | Always creates a new pet record | Submit repeated guest requests and compare |

### 8.5 Surgery Capacity / Load System

| Surgery Category | Load Points |
|---|---:|
| Minor | 1 |
| Moderate | 2 |
| Major | 3 |
| Emergency | Bypasses load limit |

| Rule | Current Logic |
|---|---|
| Daily max load | 5 points/day |
| Included appointments | Surgery appointments that are not rejected and not emergency |
| Rejected status | Not counted in capacity |
| Emergency requests | Skip capacity limit |

### 8.6 Date and Availability Restrictions

| Restriction | Current Rule |
|---|---|
| Past date | Not allowed |
| Same-week rule | Must be between today and today + 6 days |
| Closed day | Tuesday |
| Fixed holidays | New Year’s Day, All Saints’ Day, Christmas Eve, Christmas Day, New Year’s Eve |
| Holy Week blocked days | Maundy Thursday, Good Friday, Black Saturday |

### 8.7 Appointment Editing and Scheduling

| Feature | Who Can Use It | View/Page | Controller/Action | Description | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|
| Edit appointment | Client, Staff, Admin | `Views/Appointments/Edit.cshtml` | `Appointments/Edit` | Updates appointment details | Clients only own appointments and only if at least 1 day away | Edit appointment as client and staff |
| Client reschedule request | Client | Edit page | `Appointments/Edit` | Client changes preferred date/time and request returns to pending | Status reset to Pending; finalized set false | Edit as client |
| Staff/admin schedule finalization | Staff, Admin | Edit page | `Appointments/Edit` | Staff/admin adjust actual schedule and status | Staff/admin only | Change date/time as staff/admin |
| Finalization flag | Staff, Admin | Edit flow | `Appointments/Edit` | Approved/completed appointments marked finalized | Rejected becomes not finalized | Change status and inspect flags |

### 8.8 Appointment Approval and Status Actions

| Action | Who Can Use It | Controller/Action | What It Does | Notifications |
|---|---|---|---|---|
| Approve | Staff, Admin | `Appointments/Approve` | Sets status to approved/confirmed and finalized | Sends client confirmation notification |
| Reject | Staff, Admin | `Appointments/Reject` | Sets status to rejected and not finalized | Sends client decline notification |
| Complete | Staff, Admin | `Appointments/Complete` | Marks appointment completed and finalized | Sends client completed notification |

### 8.9 Appointment Cancellation

| Feature | Who Can Use It | View/Page | Controller/Action | Description | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|
| Cancel appointment | Client, Staff, Admin | `Views/Appointments/Delete.cshtml` | `Appointments/Delete`, `DeleteConfirmed` | Removes appointment | Clients cannot cancel within 1 day of schedule | Cancel as client and staff/admin |
| Staff/admin cancellation notice | Staff, Admin | Delete action | `DeleteConfirmed` | Creates cancellation notification before deletion | None beyond role | Delete staff-side appointment and check client notification |

### 8.10 UI Actions on Appointment Table

| Button/Icon | Meaning |
|---|---|
| Eye | View details |
| Pencil | Edit appointment |
| Calendar X | Cancel appointment |
| Check | Approve appointment |
| Ban | Reject appointment |
| Double check | Mark completed |

### 8.11 Client Appointment Restrictions

| Restriction | How It Works |
|---|---|
| Own appointments only | Filtered through owner relationship |
| Edit blocked within 1 day | `CanClientChangeAppointment()` |
| Cancel blocked within 1 day | `CanClientChangeAppointment()` |
| Disabled UI icons still visible | Index view shows disabled icons with explanation message |

---

## 9. Notifications

### 9.1 Notification System Overview

| Item | Details |
|---|---|
| Main table/model | `StaffNotification` |
| Used for | Staff/admin notifications and client notifications |
| Read state | `IsRead` |
| Recipient separation | `RecipientRole` + optional `UserId` |
| Linked appointment | Optional `AppointmentId` |

### 9.2 Staff/Admin Notifications

| Feature | View/Page | Controller/Action | Trigger |
|---|---|---|---|
| Notification list | `Views/Notifications/Index.cshtml` | `Notifications/Index` | Shows staff-facing notifications |
| Mark as read | Same page | `Notifications/MarkAsRead` | AJAX or normal request |
| Emergency highlight | Same page | View logic | Highlights emergency notifications |

### 9.3 Client Notifications

| Feature | View/Page | Controller/Action | Trigger |
|---|---|---|---|
| Notification list | `Views/Notifications/Client.cshtml` | `Notifications/Client` | Shows logged-in client’s notifications |
| Mark as read | Same page | `Notifications/MarkAsReadClient` | AJAX or normal request |

### 9.4 Notification Events Found in Code

| Event | Recipient |
|---|---|
| New surgery request submitted | Staff |
| New surgery request submitted and pending | Client |
| Client updated surgery request | Staff |
| Surgery approved/confirmed | Client |
| Surgery declined/rejected | Client |
| Surgery rescheduled | Client |
| Surgery completed | Client |
| Surgery cancelled by clinic/staff/admin | Client |
| Surgery tagged as emergency priority | Client |

### 9.5 Notification UI Behavior

| Notification State | UI Behavior |
|---|---|
| Unread | Normal/highlighted row |
| Read | Faded row, softer styling, still readable |
| Emergency | `Emergency Priority` label shown in staff/admin notifications |

---

## 10. Medical Records

| Feature Name | Who Can Use It | View/Page | Controller/Action | What It Does | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| View records | Client, Staff, Admin | `Views/MedicalRecords/Index.cshtml` | `MedicalRecords/Index` | Lists medical records | `MedicalRecord`, `Pet`, `Appointment` | Client only sees records for own pets | Compare client/staff views |
| Add record | Client, Staff, Admin | `Views/MedicalRecords/Create.cshtml` | `MedicalRecords/Create` | Creates record for selected pet | `MedicalRecord` | Client must choose owned pet | Create record |
| Edit record | Client, Staff, Admin | `Views/MedicalRecords/Edit.cshtml` | `MedicalRecords/Edit` | Updates record | `MedicalRecord` | Client limited to owned-pet records | Edit record |
| View details | Client, Staff, Admin | `Views/MedicalRecords/Details.cshtml` | `MedicalRecords/Details` | Shows record details | `MedicalRecord` | Client limited to owned-pet records | Open details |
| Delete record | Client, Staff, Admin | `Views/MedicalRecords/Delete.cshtml` | `MedicalRecords/Delete` | Deletes record | `MedicalRecord` | Client limited to owned-pet records | Delete record |

---

## 11. Vaccination Workflow

| Feature Name | Who Can Use It | View/Page | Controller/Action | What It Does | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|---|
| View vaccination records | Client, Staff, Admin | `Views/Vaccinations/Index.cshtml` | `Vaccinations/Index` | Lists vaccination records | `VaccinationRecord`, `Pet` | Client sees only own pet records | Compare role views |
| Add vaccination record | Client, Staff, Admin | `Views/Vaccinations/Create.cshtml` | `Vaccinations/Create` | Creates record | `VaccinationRecord` | Client limited to owned pets | Add vaccination |
| Edit vaccination record | Client, Staff, Admin | `Views/Vaccinations/Edit.cshtml` | `Vaccinations/Edit` | Updates record | `VaccinationRecord` | Client limited to owned pet records | Edit record |
| Vaccination details | Client, Staff, Admin | `Views/Vaccinations/Details.cshtml` | `Vaccinations/Details` | Shows record details | `VaccinationRecord` | Client limited to owned pet records | Open details |
| Delete vaccination record | Client, Staff, Admin | `Views/Vaccinations/Delete.cshtml` | `Vaccinations/Delete` | Deletes record | `VaccinationRecord` | Client limited to owned pet records | Delete record |

### Dashboard Vaccination Behavior
- Overdue vaccinations are shown on the client dashboard when `NextDueDate` is earlier than today.

---

## 12. Services Management

| Feature Name | Who Can Use It in Current Code | View/Page | Controller/Action | What It Does | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|
| View services | Any logged-in user | `Views/Services/Index.cshtml` | `Services/Index` | Lists services | Login required only | Open service list as each role |
| Add service | Any logged-in user | `Views/Services/Create.cshtml` | `Services/Create` | Creates new service | Login required only | Create service |
| Edit service | Any logged-in user | `Views/Services/Edit.cshtml` | `Services/Edit` | Updates service | Login required only | Edit service |
| Service details | Any logged-in user | `Views/Services/Details.cshtml` | `Services/Details` | Shows service info | Login required only | Open details |
| Delete service | Any logged-in user | `Views/Services/Delete.cshtml` | `Services/Delete` | Deletes service | Blocked if used by appointment | Try deleting used service |

### Service Behavior Notes
- New services are created as active.
- `IsWalkInOnly` and `RequiresAppointment` exist on the model.
- Appointment booking logic currently ignores most service settings and instead checks whether service name contains `"Surgery"`.

---

## 13. Clinic Information

| Feature Name | Who Can Use It | View/Page | Controller/Action | What It Does | Data Used | Restrictions | Manual Testing |
|---|---|---|---|---|---|---|
| View clinic page | Logged-in users | `Views/Clinic/Index.cshtml` | `Clinic/Index` | Displays clinic information | `ClinicInfo` model is passed, but page currently uses hard-coded display values | Login required | Open as different roles |
| Edit clinic info | Admin | `Views/Clinic/Edit.cshtml` | `Clinic/Edit` | Saves clinic information | `ClinicInfo` | Admin only | Edit and save |

---

## 14. Client-Side Restrictions and Security

### 14.1 Session Handling

| Session Key | Purpose |
|---|---|
| `UserId` | Identifies logged-in user |
| `Username` | Displays/logically tracks current username |
| `RoleId` | Determines role-based access |

### 14.2 Ownership Filtering Logic

| Data Type | How Access Is Restricted for Clients |
|---|---|
| Pets | Only pets where `Pet.OwnerId` belongs to logged-in user’s `PetOwner` |
| Appointments | Only appointments whose pet belongs to logged-in client |
| Medical Records | Only records whose `PetId` belongs to client’s pets |
| Vaccinations | Only records whose `PetId` belongs to client’s pets |
| Notifications | Only notifications with matching `UserId` and `RecipientRole = Client` |

### 14.3 Relationship Chain Used for Client Data Isolation

`User -> PetOwner -> Pet -> Appointment / MedicalRecord / VaccinationRecord`

Beginner-friendly explanation:
- The logged-in client account is stored in the `Users` table.
- That user is connected to one `PetOwner` profile.
- That pet owner profile owns one or more pets.
- Appointments, medical records, and vaccination records are connected to pets.
- Because of that chain, the system can find only the data that belongs to that client.

---

## 15. Database Relationships

### 15.1 Main Tables and Relationships

| Table/Model | Important Connections |
|---|---|
| `Role` | One role can have many users |
| `User` | Belongs to one role; may have one `PetOwner`; may create many appointments, medical records, vaccinations |
| `PetOwner` | Belongs to one user; can own many pets |
| `Pet` | Belongs to one pet owner; can have many appointments, medical records, vaccinations |
| `Service` | Can be used by many appointments |
| `AppointmentStatus` | Can be used by many appointments |
| `Appointment` | Belongs to one pet, one service, one status, one creator user |
| `MedicalRecord` | Belongs to one pet; optionally linked to an appointment |
| `VaccinationRecord` | Belongs to one pet; belongs to creator user |
| `StaffNotification` | Optional appointment link; optional client user link |
| `ReminderLog` | Belongs to one appointment |
| `ClinicInfo` | Stores clinic details |

### 15.2 Beginner-Friendly Relationship Explanation

1. A user account is created first.
2. If the user is a client, the system also creates a pet owner profile for them.
3. Pets are attached to the pet owner profile.
4. Appointments are attached to a specific pet and service.
5. Medical and vaccination records are attached to a specific pet.
6. Notifications can point either to staff as a role or to a specific client user.

---

## 16. Validation Rules Found in Code

### 16.1 Registration and Account Validation

| Field | Rule |
|---|---|
| Username | Required, unique, at least 4 characters in settings, no spaces in settings |
| Email | Unique at registration, Gmail-only in settings update |
| Contact Number | Must be valid Philippine mobile number |
| Password | Must match login hash; strong password required for change |

### 16.2 Appointment Validation

| Rule | Description |
|---|---|
| Surgery only | Only surgery services can be booked as appointments |
| Date required | Appointment/guest booking requires date |
| Time required | Appointment/guest booking requires time |
| No past dates | Surgery request cannot be booked in the past |
| 1-week window | Surgery date must be today through today + 6 days |
| Closed Tuesdays | Not allowed |
| Holiday closure | Selected holidays are blocked |
| Category required | Surgery category is required |
| Capacity check | Non-emergency daily points cannot exceed 5 |
| Client edit/cancel restriction | Changes blocked less than 1 day before appointment |

### 16.3 Deactivation Validation

| Field | Rule |
|---|---|
| Confirm Username | Must match current username |
| Confirm Password | Must match current password hash |
| Confirmation Text | Must exactly equal `DEACTIVATE` |

---

## 17. UI Buttons and Actions

### 17.1 Shared UI Behavior

| UI Element | Behavior |
|---|---|
| Navbar | Role-based links shown depending on login state |
| Logout button | Shown in top header for logged-in users |
| Toast messages | Success, error, warning, and notice popups via TempData/ViewBag |
| Table action links | Details/Edit/Delete open inside modal via `site.js` |
| Confirmation overlays | Used for destructive actions |
| Table pagination | Standard tables paginate automatically after 8 rows |
| Mobile nav | Toggle menu for smaller screens |

### 17.2 Notifications UI

| Action | Result |
|---|---|
| Mark as read | Status changes to `Read`, row fades visually |
| Emergency notification | Highlighted with `Emergency Priority` label |

---

## 18. Reminder Logs

When a new appointment is created, the system automatically creates reminder log entries:

| Reminder Type | Purpose |
|---|---|
| Confirmation | Confirms appointment request was received |
| Reminder 2 Days Before | Pre-appointment reminder |
| Reminder 1 Day Before | Day-before reminder |
| Follow-up After Visit | Post-visit follow-up reminder |

### Important Note
The code creates reminder log records, but no automated sending job was found in the inspected project files.

---

## 19. Testing Checklist

### 19.1 Authentication

- [ ] Register a client account  
  **Steps:** Open Register page, fill all fields, submit.  
  **Expected result:** New client user and pet owner are created.

- [ ] Log in as client  
  **Steps:** Enter client credentials.  
  **Expected result:** Redirect to client dashboard.

- [ ] Log in as staff  
  **Steps:** Enter staff credentials.  
  **Expected result:** Redirect to staff dashboard.

- [ ] Log in as admin  
  **Steps:** Enter admin credentials.  
  **Expected result:** Redirect to admin dashboard.

- [ ] Block inactive account login  
  **Steps:** Deactivate an account, then try logging in.  
  **Expected result:** Login fails with deactivated-account message.

### 19.2 Account Settings

- [ ] Update personal information  
  **Steps:** Change name/contact/username/email and save.  
  **Expected result:** New values persist.

- [ ] Change password  
  **Steps:** Enter current password, strong new password, confirm.  
  **Expected result:** Password successfully changes.

- [ ] Reject weak password  
  **Steps:** Enter a weak password.  
  **Expected result:** Validation error appears.

- [ ] Deactivate account  
  **Steps:** Enter matching username, password, and `DEACTIVATE`.  
  **Expected result:** Account becomes inactive and user is logged out.

### 19.3 Pet Management

- [ ] Add pet  
  **Steps:** Open Pets > Create and submit.  
  **Expected result:** Pet appears in pet list.

- [ ] Edit pet  
  **Steps:** Open pet edit page, change details, save.  
  **Expected result:** Pet details update.

- [ ] Delete pet  
  **Steps:** Delete a pet.  
  **Expected result:** Pet and linked operational data are removed correctly.

- [ ] Client ownership restriction on pets  
  **Steps:** Try opening another client’s pet URL.  
  **Expected result:** Access is blocked.

### 19.4 Surgery Appointment Workflow

- [ ] Create surgery request as client  
  **Steps:** Open Appointments > Create, submit all fields.  
  **Expected result:** Pending surgery request is created.

- [ ] Create surgery request as guest  
  **Steps:** Use guest booking form and submit.  
  **Expected result:** Guest user, pet, and appointment are created.

- [ ] Reject non-surgery booking  
  **Steps:** Try non-surgery selection in appointment flow.  
  **Expected result:** System blocks booking and says other services are walk-in only.

- [ ] Block Tuesday booking  
  **Steps:** Choose Tuesday date.  
  **Expected result:** Validation error appears.

- [ ] Block holiday booking  
  **Steps:** Choose listed holiday.  
  **Expected result:** Validation error appears.

- [ ] Block date outside 1-week range  
  **Steps:** Choose date beyond 6 days ahead.  
  **Expected result:** Validation error appears.

- [ ] Enforce surgery category  
  **Steps:** Submit with no category.  
  **Expected result:** Category validation error.

- [ ] Enforce capacity points  
  **Steps:** Fill a day beyond 5 non-emergency surgery points.  
  **Expected result:** Capacity validation error.

- [ ] Emergency bypass  
  **Steps:** Submit emergency request on fully loaded day.  
  **Expected result:** Request still accepted.

- [ ] Client edit allowed before 1 day  
  **Steps:** Edit own future appointment more than 1 day away.  
  **Expected result:** Edit works and request returns to pending.

- [ ] Client edit blocked within 1 day  
  **Steps:** Try editing own appointment less than 1 day away.  
  **Expected result:** Edit is blocked.

- [ ] Client cancel blocked within 1 day  
  **Steps:** Try deleting own appointment less than 1 day away.  
  **Expected result:** Cancel is blocked.

- [ ] Staff approve appointment  
  **Steps:** Click Approve in appointments list.  
  **Expected result:** Status changes and client gets notification.

- [ ] Staff reject appointment  
  **Steps:** Click Reject.  
  **Expected result:** Status changes and client gets notification.

- [ ] Staff complete appointment  
  **Steps:** Click Complete.  
  **Expected result:** Status changes and client gets notification.

- [ ] Staff/admin reschedule appointment  
  **Steps:** Edit actual appointment date/time.  
  **Expected result:** Client sees updated schedule and gets reschedule notification.

### 19.5 Notifications

- [ ] Staff notification on new request  
  **Steps:** Submit new surgery request.  
  **Expected result:** Staff notification appears.

- [ ] Client notification on pending request  
  **Steps:** Submit request as client.  
  **Expected result:** Client sees pending notification.

- [ ] Client notification on approve/reject/complete/reschedule/cancel  
  **Steps:** Perform each staff/admin action.  
  **Expected result:** Correct client message appears.

- [ ] Mark notification as read  
  **Steps:** Click check icon.  
  **Expected result:** Badge changes to Read and row becomes faded.

- [ ] Emergency highlight  
  **Steps:** Submit emergency surgery request.  
  **Expected result:** Staff/admin notification shows `Emergency Priority`.

### 19.6 Medical Records and Vaccinations

- [ ] Create medical record  
  **Steps:** Add a record for a pet.  
  **Expected result:** Record saved and listed.

- [ ] Edit medical record  
  **Steps:** Update fields and save.  
  **Expected result:** Changes persist.

- [ ] Delete medical record  
  **Steps:** Delete a record.  
  **Expected result:** Record removed.

- [ ] Create vaccination record  
  **Steps:** Add vaccine entry.  
  **Expected result:** Record saved.

- [ ] Edit vaccination record  
  **Steps:** Change vaccine details and save.  
  **Expected result:** Record updated.

- [ ] Delete vaccination record  
  **Steps:** Delete a vaccination record.  
  **Expected result:** Record removed.

### 19.7 Admin and Staff Operations

- [ ] Open admin users page  
  **Steps:** Log in as admin and click `Users`.  
  **Expected result:** User table appears.

- [ ] Deactivate another user  
  **Steps:** Click deactivate on a user.  
  **Expected result:** User status becomes inactive.

- [ ] Restore inactive user  
  **Steps:** Click restore.  
  **Expected result:** User status becomes active.

- [ ] Prevent admin self-deactivation  
  **Steps:** Check current account row.  
  **Expected result:** It shows `Current account`, not deactivate button.

- [ ] Edit clinic info as admin  
  **Steps:** Open clinic page and edit.  
  **Expected result:** Data saves successfully.

### 19.8 Responsive / Accessibility Checks

- [ ] Desktop layout  
  **Steps:** Test around 1366px and 1920px width.  
  **Expected result:** Navigation, sections, tables, and forms display properly.

- [ ] Laptop layout  
  **Steps:** Test around 1024px and 1280px width.  
  **Expected result:** No broken alignment or hidden key actions.

- [ ] Tablet layout  
  **Steps:** Test around 768px width.  
  **Expected result:** Navbar collapses properly, pages stay usable.

- [ ] Mobile layout  
  **Steps:** Test around 375px width.  
  **Expected result:** Navigation, forms, and cards remain usable.

- [ ] Mobile booking form  
  **Steps:** Open surgery booking page on phone width.  
  **Expected result:** Form fields stack properly and remain readable.

- [ ] Mobile modal  
  **Steps:** Open details/edit/delete modal on phone width.  
  **Expected result:** Modal fits screen and actions are clickable.

- [ ] Mobile tables  
  **Steps:** Open appointments/pets/records lists on phone width.  
  **Expected result:** Pagination and table panels remain usable.

---

## 20. Possible Issues or Missing Features

| Issue / Concern | Details |
|---|---|
| Service management access is broad | `ServicesController` currently allows any logged-in user, including clients, to access service CRUD |
| Medical/vaccination CRUD for clients may be too broad | Clients are ownership-limited but still allowed to create/edit/delete these records in current code |
| Clinic display is hard-coded | `Views/Clinic/Index.cshtml` currently displays hard-coded clinic values instead of fully using the saved `ClinicInfo` model |
| Landing page is mostly static | Services, prices, contacts, and articles are not driven from database |
| Service appointment logic depends on service name | Appointment eligibility checks if service name contains `"Surgery"` instead of using only model flags |
| Reminder logs are not actually sent | Records are created, but no sending scheduler/service was found in inspected files |
| Guest booking duplicates pets | Repeated guest bookings create new pet records instead of reusing existing ones |
| Role checks use numeric IDs directly | Admin=1, Staff=2, Client=3 is assumed throughout code |
| Manual authorization style | Access control is repeated in many controllers instead of centralized policies |
| Terminology varies | Approved/Confirmed and Rejected/Declined are both used in different places |

---

## 21. Documentation-Friendly Summary

VetClinicSystem is a role-based veterinary clinic management website that supports public visitors, clients, staff, and administrators. The system focuses on surgery request scheduling, pet management, medical history tracking, vaccination record management, user account handling, clinic notifications, and role-based dashboards. Clients can register, maintain their pet information, request surgery appointments, monitor reminders and updates, and access records related to their own pets. Staff and admins manage operational clinic tasks such as appointment approval, scheduling, records, vaccination data, and notifications. Admins additionally manage user accounts and overall dashboard reporting.

The appointment workflow is centered on surgery requests. Clients or guests submit a preferred surgery date and time, a surgery category, an emergency priority option, and case notes. The clinic then reviews and finalizes the actual schedule. Surgery requests are controlled by availability rules such as closed Tuesdays, holiday blocks, a one-week booking window, and a daily surgery load system. Non-emergency requests are limited to 5 surgery points per day, where minor, moderate, and major surgeries consume 1, 2, and 3 points respectively. Emergency requests bypass that capacity rule.

The system uses a relational data structure where users are connected to pet owner profiles, pet owners are connected to pets, and pets are connected to appointments, medical records, and vaccination records. This relationship is used to make sure clients only see their own data. Notifications are also separated by role, allowing staff/admin users to receive clinic operation alerts while clients receive appointment status and scheduling updates. Overall, the system combines clinic operations, client communication, and pet record management into one centralized website.

