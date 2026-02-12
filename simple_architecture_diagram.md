# Gsystem - Simple Architecture Diagram

## Simple System Architecture

```mermaid
graph LR
    subgraph "USERS"
        Admin["Admin"]
        Teacher["Teacher"] 
        Student["Student"]
    end
    
    subgraph "FRONTEND"
        Web["Web Browser<br/>Blazor WebAssembly"]
        Role["Role Based<br/>Access Control"]
    end
    
    subgraph "BACKEND"
        API["API Gateway<br/>ASP.NET Core"]
        Encrypt["Encrypt<br/>Authentication"]
        Query["Query<br/>Data Processing"]
        Services["Services<br/>Business Logic"]
    end
    
    subgraph "DATABASE CLOUD"
        DB["MySQL Database<br/>eprofilr"]
    end
    
    %% User connections
    Admin --> Web
    Teacher --> Web
    Student --> Web
    
    %% Frontend connections
    Web --> Role
    Role --> API
    
    %% Backend connections
    API --> Encrypt
    API --> Query
    API --> Services
    Encrypt --> Query
    Query --> DB
    Services --> DB
    
    %% Response flow
    DB --> Query
    Query --> Encrypt
    Encrypt --> API
    API --> Role
    Role --> Web
    
    %% Styling
    classDef userStyle fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef frontendStyle fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef backendStyle fill:#e8f5e8,stroke:#388e3c,stroke-width:2px
    classDef dbStyle fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    
    class Admin,Teacher,Student userStyle
    class Web,Role frontendStyle
    class API,Encrypt,Query,Services backendStyle
    class DB dbStyle
```

## Data Flow:
1. **Users** (Admin, Teacher, Student) access the system
2. **Frontend** (Blazor WebAssembly) handles user interface
3. **API Gateway** receives requests and routes them
4. **Authentication** encrypts and validates user data
5. **Services** process business logic
6. **Database** stores and retrieves data
7. **Response** flows back through the same path

## Key Components:
- **Users**: Admin, Teacher, Student roles
- **Frontend**: Blazor WebAssembly web application
- **Backend**: ASP.NET Core API with authentication
- **Database**: MySQL database for data storage
