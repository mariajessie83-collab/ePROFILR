# Student Profile and Case Records Implementation

## Overview
This implementation provides a comprehensive system for managing student disciplinary case records. The system includes form creation, data management, and dashboard viewing capabilities.

## Features Implemented

### 1. Student Profile and Case Records Form (`/student-profile-case-record`)
- **Student Offender Information**
  - Name of Student Offender (Last Name, First Name, MI.) - Required
  - Grade Level - Dropdown (Grade 11, Grade 12) - Required
  - Track/Strand - Dropdown (STEM, ABM, HUMSS, GAS, TVL options) - Required
  - Section - Text input - Required

- **Adviser Information**
  - Name of Adviser - Required
  - **Smart Teacher Search**: As user types, system provides suggestions from teacher database
  - Manual entry also supported if teacher not found in database

- **Parent/Guardian Information**
  - Name of Parent/Guardian - Required
  - Contact Number - Required with Philippine mobile number validation (09XXXXXXXXX format)

- **Offense Details**
  - Date of Offense - Required
  - Level of Offense - Dropdown (Prohibited Acts, Minor, Major) - Required
  - Violation Committed - Dropdown populated from database - Required
  - Other Violation Description - Conditional field (appears only when "Other" is selected) - Required if Other
  - Number of Offense - Dropdown (First Offense, Second Offense, Third Offense) - Required

- **Agreement Details**
  - Details of Agreement - Required
  - POD In-Charge - Auto-populated from current logged-in user

### 2. Database Structure
- **StudentProfileCaseRecords Table**: Main table storing all case record data
- **ViolationTypes Table**: Reference table for violation types and categories
- **Proper indexing**: Optimized for performance on common queries
- **Foreign key relationships**: Links to Users table for audit trail

### 3. API Endpoints
- `GET /api/studentprofilecaserecord/violation-types` - Get list of violation types
- `GET /api/studentprofilecaserecord/search-teachers?searchTerm={term}` - Search teachers
- `GET /api/studentprofilecaserecord` - Get case records with pagination and filtering
- `GET /api/studentprofilecaserecord/{recordId}` - Get specific case record
- `POST /api/studentprofilecaserecord` - Create new case record

### 4. Dashboard (`/case-records-dashboard`)
- View all case records in a table format
- Filter by status (Active, Resolved, Closed)
- Pagination support
- Status and level badges for easy identification
- Quick actions to view record details

## Technical Implementation

### Models
- `StudentProfileCaseRecordModel`: Main data model with validation attributes
- `StudentProfileCaseRecordRequest`: API request model
- `TeacherSearchResult`: Teacher search response model
- `StudentProfileCaseRecordSummary`: Dashboard display model

### Services
- `StudentProfileCaseRecordService`: Business logic for case record operations
- Teacher search functionality with database queries
- Violation types management
- CRUD operations for case records

### Controllers
- `StudentProfileCaseRecordController`: RESTful API endpoints
- Proper error handling and validation
- JSON response formatting

### Frontend Components
- **StudentProfileCaseRecord.razor**: Main form component
- **CaseRecordsDashboard.razor**: Dashboard for viewing records
- Real-time teacher search with suggestions
- Form validation with user-friendly error messages
- Responsive design with modern UI

## Key Features

### 1. Smart Teacher Search
- As user types in adviser name field, system searches teacher database
- Shows suggestions with teacher name, position, and grade handled
- Users can click suggestion or continue typing manually
- Fallback to manual entry if no matches found

### 2. Dynamic Violation Types
- Violation types stored in database for easy management
- Dropdown populated dynamically from database
- Support for custom "Other" violations with description field

### 3. Conditional Field Display
- "Other Violation Description" field only appears when "Other" is selected
- Required validation applied conditionally

### 4. Phone Number Validation
- Real-time validation for Philippine mobile numbers
- Format: 09XXXXXXXXX (11 digits starting with 09)
- Visual feedback (green checkmark or red warning)

### 5. Auto-population
- POD In-Charge field automatically populated from logged-in user
- Reduces manual data entry and ensures accuracy

### 6. Authentication Integration
- Form and dashboard require user authentication
- Uses existing authentication system
- Different access levels for teachers vs students

## Database Schema

### StudentProfileCaseRecords Table
```sql
- RecordID (Primary Key)
- StudentOffenderName
- GradeLevel
- TrackStrand
- Section
- AdviserName
- ParentGuardianName
- ParentGuardianContact
- DateOfOffense
- LevelOfOffense (ENUM: Prohibited Acts, Minor, Major)
- ViolationCommitted
- OtherViolationDescription
- NumberOfOffense (ENUM: First Offense, Second Offense, Third Offense)
- DetailsOfAgreement
- PODInCharge
- DateCreated
- Status (ENUM: Active, Resolved, Closed)
- CreatedBy, UpdatedBy (Foreign Keys to Users)
- IsActive
```

### ViolationTypes Table
```sql
- ViolationID (Primary Key)
- ViolationName
- ViolationCategory (ENUM: Prohibited Acts, Minor, Major)
- Description
- IsActive
- DateCreated
```

## Usage Instructions

### For Teachers/Administrators:
1. Navigate to `/student-profile-case-record`
2. Fill in all required fields
3. Use teacher search for adviser name
4. Select appropriate violation type and level
5. Provide agreement details
6. Submit form to create case record

### For Viewing Records:
1. Navigate to `/case-records-dashboard`
2. Use status filter to view specific records
3. Click "View Details" for individual record information
4. Use pagination to browse through records

## Security Features
- Authentication required for all operations
- Input validation and sanitization
- SQL injection prevention through parameterized queries
- XSS protection through proper encoding

## Future Enhancements
- Email notifications for case record creation
- PDF report generation
- Advanced search and filtering options
- Case record status workflow management
- Integration with existing student management system
- Mobile-responsive design improvements

## Files Created/Modified
- `SharedProject/StudentProfileCaseRecord.cs` - Data models
- `Gsystem/Database/CreateStudentProfileCaseRecords_Table.sql` - Database schema
- `Server/Services/StudentProfileCaseRecordService.cs` - Business logic
- `Server/Controllers/StudentProfileCaseRecordController.cs` - API endpoints
- `Server/Program.cs` - Service registration
- `Gsystem/Pages/StudentProfileCaseRecord.razor` - Main form
- `Gsystem/Pages/CaseRecordsDashboard.razor` - Dashboard

## Testing
To test the implementation:
1. Run the database creation script
2. Start the application
3. Login as a teacher
4. Navigate to `/student-profile-case-record`
5. Fill out and submit the form
6. View records in `/case-records-dashboard`
