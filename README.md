# env-init

A Python tool for initializing and configuring Keycloak environments and managing environment files. This tool automates the setup of Keycloak realms, clients, roles, users, and configuration management.

## Overview

`env-init` is designed to streamline the setup of Keycloak identity management infrastructure. It provides:

- **Automated Keycloak Setup**: Creates realms, clients, roles, and users based on configuration files
- **Service Account Management**: Configures service accounts with proper role assignments
- **Environment Configuration**: Manages environment variables and secrets (client secrets, signing keys)
- **Idempotent Operations**: Safe to run multiple times—checks for existing resources before creation
- **Token Lifespan Management**: Configures access and refresh token lifespans at both realm and client levels

## Usage Guide

### Prerequisites

- Python 3.11 or higher
- Docker (for containerized deployment)
- Running Keycloak instance with admin credentials
- `requests` library (installed via requirements)

### Installation

1. **Install dependencies**:

   ```bash
   pip install requests psycopg[binary]
   ```

2. **Verify installation**:
   ```bash
   python main.py --help  # if help is implemented
   ```

### Configuration Files

Before running the tool, configure two JSON files:

#### 1. Keycloak Configuration (`config/keycloak.json`)

```json
{
  "base_url": "http://keycloak:8080",
  "admin_user": "admin",
  "admin_pass": "admin-password",
  "realm_name": "my-realm",
  "client_id": "my-client",
  "redirect_uris": ["http://localhost:3000/*"],
  "web_origins": ["http://localhost:3000"],
  "roles": ["admin", "user", "editor"],
  "enable_service_account": true,
  "access_token_lifespan": 600,
  "refresh_token_lifespan": 3600,
  "frontend_url": "http://keycloak:8080",
  "users": [
    {
      "username": "testuser",
      "password": "testpass123",
      "firstName": "Test",
      "lastName": "User",
      "email": "test@example.com",
      "roles": ["user"]
    }
  ]
}
```

**Configuration Fields**:

- `base_url`: Keycloak server URL
- `admin_user`: Keycloak master realm admin username
- `admin_pass`: Keycloak master realm admin password
- `realm_name`: Name of the realm to create/use
- `client_id`: OAuth2/OpenID Connect client ID
- `redirect_uris`: Allowed redirect URIs for OAuth flows
- `web_origins`: Allowed web origins for CORS
- `roles`: List of roles to create in the realm
- `enable_service_account`: Whether to enable service accounts for the client
- `access_token_lifespan`: Token lifespan in seconds
- `refresh_token_lifespan`: Refresh token lifespan in seconds
- `frontend_url`: Frontend URL for realm configuration
- `users`: List of users to create with roles

#### 2. Environment File Configuration (`config/env.json`)

```json
{
  "files": {
    "env_file": "./config/env.json",
    "secrets_file": "./config/secrets.json"
  },
  "env": {
    "client_secret": "env_file",
    "signing_key": "secrets_file"
  }
}
```

**Configuration Fields**:

- `files`: Maps reference names to actual file paths where environment values will be stored
- `env`: Maps environment variable names to file references where values should be stored

### Running the Tool

#### Local Execution

```bash
# Set up Keycloak realm and store configuration
python main.py
```

The script will:

1. Read configuration files (`config/keycloak.json` and `config/env.json`)
2. Connect to Keycloak using admin credentials
3. Create realm if it doesn't exist
4. Create client with specified options
5. Enable service account roles if configured
6. Create roles and users
7. Configure token lifespans
8. Extract and store client secret and signing key in specified files

#### Docker Execution

```bash
# Build the Docker image
docker build -t env-init:latest .

# Run the container
docker run env-init:latest
```

#### Multi-environment Setup

For multiple environments (e.g., dragon environment):

```bash
# Create environment-specific config
mkdir -p config/dragon
cp config/keycloak.json config/dragon/
cp config/env.json config/dragon/

# Modify configs for the specific environment
# Then run setup for that environment
```

### Output

When successful, the tool outputs:

```
Setting up Keycloak...
Connecting to Keycloak at http://keycloak:8080 with admin user 'admin'
Checking/Creating realm 'my-realm'...
Realm 'my-realm' created.
Checking/Creating client 'my-client'...
Client 'my-client' created in realm 'my-realm'.
Enabling service account for client 'my-client'...
Service account enabled and roles assigned for client 'my-client'.
Checking/Creating roles and users in realm 'my-realm'...
...
Keycloak setup completed.
```

## Maintenance Guide

### Project Structure

```
env-init/
├── main.py              # Entry point
├── setup.py             # Core setup functions
├── env.py               # Environment file utilities
├── keycloak.py          # Keycloak API interactions
├── utils.py             # Utility functions
├── Dockerfile           # Container definition
├── config/
│   ├── env.json         # Environment configuration template
│   ├── keycloak.json    # Keycloak configuration template
│   └── dragon/          # Dragon environment configs
└── README.md            # This file
```

### Key Components

#### `main.py`

- **Purpose**: Entry point that orchestrates the setup process
- **Responsibilities**: Load configuration files and call setup functions in order

#### `setup.py`

- **Purpose**: Implements the main setup logic
- **Key Functions**:
  - `setup_keycloak()`: Orchestrates Keycloak realm, client, roles, and user creation
  - `setup_env()`: Manages environment file configuration and secret extraction

#### `keycloak.py`

- **Purpose**: Wraps Keycloak REST API calls
- **Key Functions**:
  - `get_admin_token()`: Authenticates with Keycloak master realm
  - `create_realm()`, `create_client()`, `create_role()`, `create_user()`
  - `assign_role()`, `assign_realm_management_roles_to_service_account()`
  - `get_client_secret()`, `get_signing_key()`
  - Check functions: `is_realm_exists()`, `is_client_exists()`, etc.

#### `env.py`

- **Purpose**: File-based environment variable management
- **Key Functions**:
  - `json_env()`: Read a value from JSON file
  - `json_set_env()`: Write a value to JSON file
  - `json_create_empty_env()`: Initialize empty environment file

#### `utils.py`

- **Purpose**: Utility functions for service health checks
- **Key Functions**:
  - `wait_for_http_ready()`: Poll HTTP endpoints until ready
  - `wait_for_keycloak_ready()`: Keycloak-specific readiness check

### Common Maintenance Tasks

#### Adding a New Role

1. Edit `config/keycloak.json`:

   ```json
   "roles": ["admin", "user", "editor", "viewer"]
   ```

2. Run the tool:
   ```bash
   python main.py
   ```

#### Adding a New User

1. Edit `config/keycloak.json`:

   ```json
   "users": [
       {
           "username": "newuser",
           "password": "newpass123",
           "roles": ["user", "editor"]
       }
   ]
   ```

2. Run the tool:
   ```bash
   python main.py
   ```

#### Changing Token Lifespans

Update the Keycloak configuration:

```json
{
  "access_token_lifespan": 1200,
  "refresh_token_lifespan": 7200
}
```

Run the setup again to apply changes.

#### Debugging Connection Issues

1. **Check Keycloak connectivity**:

   ```bash
   # Manually test the connection
   curl http://keycloak:8080/realms/master/.well-known/openid-configuration
   ```

2. **Verify credentials**:
   - Ensure `admin_user` and `admin_pass` are correct
   - Check if the admin user exists in Keycloak master realm

3. **Check configuration files**:
   ```bash
   # Validate JSON syntax
   python -m json.tool config/keycloak.json
   python -m json.tool config/env.json
   ```

### Troubleshooting

| Issue                         | Solution                                                                 |
| ----------------------------- | ------------------------------------------------------------------------ |
| "Connection refused"          | Verify Keycloak is running and `base_url` is correct                     |
| "Invalid credentials"         | Check admin username and password in configuration                       |
| "Realm already exists"        | This is normal—tool skips existing resources                             |
| "File not found"              | Ensure config JSON files exist in `config/` directory                    |
| "Unable to get client secret" | Verify client was created successfully; check service account is enabled |

### Running Tests/Validation

To validate configuration before running:

```bash
# Syntax check
python -m json.tool config/keycloak.json
python -m json.tool config/env.json

# Dry run (if implemented)
# python main.py --dry-run
```

### Dependencies Management

**Current Dependencies**:

- `requests`: HTTP library for Keycloak API calls
- `psycopg[binary]`: PostgreSQL driver (included for database integration)

**To update dependencies**:

1. Edit dependency requirements in this README
2. Update Dockerfile RUN pip install commands
3. Update local environment: `pip install --upgrade <package>`

### Performance Considerations

- **Idempotent Design**: Checks for existing resources before creation—safe to run multiple times
- **Rate Limiting**: Keycloak may throttle requests; tool includes reasonable timeouts
- **Service Readiness**: `wait_for_keycloak_ready()` ensures Keycloak is ready before attempting setup

### Extending the Tool

#### Adding Support for New Keycloak Features

1. Add new API wrapper functions in `keycloak.py`
2. Update `setup.py` to call the new functions
3. Add configuration options to `config/keycloak.json`
4. Update this README with new configuration fields

#### Adding New Environment Variables

1. Define file mappings in `config/env.json` under `files`
2. Add the variable to the `env` section
3. Implement value extraction logic in `setup_env()` in `setup.py`

### Environment Variants

The tool supports environment-specific configurations through subdirectories:

```bash
config/
├── keycloak.json      # Default/prod
├── env.json
└── dragon/            # Dragon environment
    ├── keycloak.json
    └── env.json
```

To use a specific environment, modify `main.py` to load from the appropriate path.

## Related Resources

- [Keycloak Official Documentation](https://www.keycloak.org/documentation)
- [Keycloak Admin REST API](https://www.keycloak.org/docs/latest/server_admin/#admin-rest-api)
- [OpenID Connect & OAuth 2.0](https://openid.net/connect/)
