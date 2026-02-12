# Gsystem Complete System Design

## Main Architecture Diagram

```mermaid
graph TD
    %% Users
    A[Admin/POD] --> B[Blazor WebAssembly]
    C[Student/Victim] --> B
    D[Teacher/Victim] --> B
    E[Parents] --> B
    
    %% Frontend Components
    B --> F[Role-based Access Controller]
    B --> G[Sign Up Process]
    B --> H[Login Process]
    B --> I[Form Fill Process]
    B --> J[Picture Upload Process]
    B --> K[POD Dashboard]
    
    %% Backend Components
    F --> L[API Gateway ASP.NET Core]
    G --> L
    H --> L
    I --> L
    J --> L
    K --> L
    
    L --> M[Encrypt Authentication]
    M --> N[Query Data Processing]
    N --> O[Services]
    
    %% Services
    O --> P[User Service]
    O --> Q[Form Service]
    O --> R[File Service]
    O --> S[Picture Service]
    O --> T[SMS Service]
    O --> U[Email Service]
    
    %% Database
    P --> V[MySQL Database]
    Q --> V
    R --> V
    S --> V
    T --> V
    U --> V
    
    %% SMS Flow
    T --> W[Format Message]
    W --> X[Send SMS]
    X --> Y[Track Delivery]
    Y --> Z[Parents Receive SMS]
    
    %% Database Tables
    V --> AA[Users Table]
    V --> BB[Forms Table]
    V --> CC[Files Table]
    V --> DD[Pictures Table]
    V --> EE[SMS Table]
    V --> FF[Cases Table]
    V --> GG[Respondents Table]
    V --> HH[Parents Table]
    
    %% Complaint Flow
    C --> II[Sign Up] --> JJ[Login] --> KK[Fill Form] --> LL[Upload Picture] --> MM[Submit Report]
    MM --> NN[POD Review] --> OO[Identify Respondent] --> PP[Process Case] --> QQ[Send SMS to Parents]
    
    %% Styling
    classDef userClass fill:#e1f5fe
    classDef frontendClass fill:#f3e5f5
    classDef backendClass fill:#e8f5e8
    classDef databaseClass fill:#fff3e0
    classDef smsClass fill:#ffebee
    
    class A,C,D,E userClass
    class B,F,G,H,I,J,K frontendClass
    class L,M,N,O,P,Q,R,S,T,U backendClass
    class V,AA,BB,CC,DD,EE,FF,GG,HH databaseClass
    class W,X,Y,Z,QQ smsClass
```

## Complete Process Flow

```mermaid
sequenceDiagram
    participant V as Victim (Student/Teacher)
    participant F as Frontend (Blazor)
    participant B as Backend (API)
    participant D as Database (MySQL)
    participant P as POD (Admin)
    participant Pa as Parents
    
    V->>F: 1. Sign Up
    F->>B: 2. Validate User
    B->>D: 3. Store User Data
    
    V->>F: 4. Login
    F->>B: 5. Authenticate
    B->>D: 6. Verify Credentials
    
    V->>F: 7. Fill Complaint Form
    F->>B: 8. Validate Form Data
    B->>D: 9. Store Form Data
    
    V->>F: 10. Upload Picture
    F->>B: 11. Process File
    B->>D: 12. Store Picture
    
    V->>F: 13. Submit Report
    F->>B: 14. Process Submission
    B->>D: 15. Store Case
    
    P->>B: 16. Review Case
    B->>D: 17. Get Case Details
    D-->>B: 18. Return Case Data
    B-->>P: 19. Display Case
    
    P->>B: 20. Identify Respondent
    B->>D: 21. Get Respondent Info
    D-->>B: 22. Return Respondent Data
    
    P->>B: 23. Get Parent Contact
    B->>D: 24. Query Parent Data
    D-->>B: 25. Return Parent Info
    
    P->>B: 26. Send SMS
    B->>B: 27. Format Message
    B->>Pa: 28. Send SMS to Parents
    Pa-->>B: 29. SMS Delivered
    
    B->>D: 30. Log SMS Status
    B->>D: 31. Update Case Status
```

## System Features

### Users
- **Admin/POD**: Manages complaints and sends SMS
- **Student/Victim**: Files complaints
- **Teacher/Victim**: Files complaints  
- **Parents**: Receive SMS notifications

### Frontend (Blazor WebAssembly)
- **Sign Up Process**: User registration
- **Login Process**: User authentication
- **Form Fill Process**: Complaint form submission
- **Picture Upload Process**: File upload functionality
- **POD Dashboard**: Admin interface for case management

### Backend (ASP.NET Core)
- **API Gateway**: Routes requests
- **Authentication**: JWT token management
- **Data Processing**: Query and validation
- **Services**: Business logic (User, Form, File, Picture, SMS, Email)

### Database (MySQL)
- **Users Table**: User accounts
- **Forms Table**: Complaint forms
- **Files Table**: Uploaded files
- **Pictures Table**: Image uploads
- **SMS Table**: SMS logs
- **Cases Table**: Complaint cases
- **Respondents Table**: Accused persons
- **Parents Table**: Parent contact info

### SMS Integration
- **Format Message**: Prepare SMS content
- **Send SMS**: Deliver to parents
- **Track Delivery**: Monitor SMS status
- **Parents Receive**: SMS notification

## Complete Flow Summary

1. **Victim** (Student/Teacher) → **Sign Up** → **Login** → **Fill Form** → **Upload Picture** → **Submit Report**
2. **POD** (Admin) → **Review Case** → **Identify Respondent** → **Get Parent Contact** → **Send SMS**
3. **Parents** → **Receive SMS** → **Respond to SMS**
4. **System** → **Log SMS Status** → **Update Case Status**

This is the complete system design with all flows, methods, and SMS integration!
