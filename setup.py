from env import json_create_empty_env, json_env, json_set_env
from keycloak import assign_realm_management_roles_to_service_account, assign_role, create_client, create_realm, create_role, create_user, get_admin_token, get_client_secret, get_signing_key, is_client_exists, is_realm_exists, is_role_exists, is_user_exists, update_client_settings_attributes, update_realm_attribute_settings, update_realm_settings

def setup_keycloak(settings):
    baseUrl = settings["base_url"]
    admin_user = settings["admin_user"]
    admin_pass = settings["admin_pass"]
    realm_name = settings["realm_name"]
    client_id = settings["client_id"]
    client_options = settings.get("client_options", {})
    users = settings.get("users", [])
    roles = settings.get("roles", [])

    print("Setting up Keycloak...")
    print(f"Connecting to Keycloak at {baseUrl} with admin user '{admin_user}'")
    token = get_admin_token(baseUrl, admin_user, admin_pass)

    print(f"Checking/Creating realm '{realm_name}'...")
    if not is_realm_exists(baseUrl, token, realm_name):
        create_realm(baseUrl, token, realm_name)
        print(f"Realm '{realm_name}' created.")
    else:
        print(f"Realm '{realm_name}' already exists.")

    print(f"Checking/Creating client '{client_id}'...")
    if not is_client_exists(baseUrl, token, realm_name, client_id):
        create_client(baseUrl, token, realm_name, client_id, client_options)
        print(f"Client '{client_id}' created in realm '{realm_name}'.")
    else:
        print(f"Client '{client_id}' already exists in realm '{realm_name}'.")

    if (client_options.get("is_service_account_enabled")):
        print(f"Enabling service account for client '{client_id}'...")
        assign_realm_management_roles_to_service_account(baseUrl, token, realm_name, client_id)
        print(f"Service account enabled and roles assigned for client '{client_id}'.")

    print(f"Checking/Creating roles and users in realm '{realm_name}'...")
    for role_name in roles:
        if not is_role_exists(baseUrl, token, realm_name, role_name):
            create_role(baseUrl, token, realm_name, role_name)
            print(f"Role '{role_name}' created in realm '{realm_name}'.")
        else:
            print(f"Role '{role_name}' already exists in realm '{realm_name}'.")
    
    print(f"Configuring realm settings '{realm_name}'...")
    realm_access_token_lifespan = settings.get("access_token_lifespan")
    if realm_access_token_lifespan is not None:
        print(f" - Setting access token lifespan to {realm_access_token_lifespan} seconds")
        update_realm_settings(baseUrl, token, realm_name, {"accessTokenLifespan": realm_access_token_lifespan})
    
    realm_refresh_token_lifespan = settings.get("refresh_token_lifespan")
    if realm_refresh_token_lifespan is not None:
        print(f" - Setting refresh token lifespan to {realm_refresh_token_lifespan} seconds")
        update_realm_settings(baseUrl, token, realm_name, {"ssoSessionIdleTimeout": realm_refresh_token_lifespan})
        update_realm_settings(baseUrl, token, realm_name, {"ssoSessionMaxLifespan": realm_refresh_token_lifespan})

    frontend_url = settings.get("frontend_url")
    if frontend_url is not None:
        print(f" - Setting frontend URL to {frontend_url}")
        update_realm_attribute_settings(baseUrl, token, realm_name, {"frontendUrl": frontend_url})
    
    print(f"Configuring client settings for client '{client_id}'...")
    client_access_token_lifespan = settings.get("client_access_token_lifespan")
    client_refresh_token_lifespan = settings.get("client_refresh_token_lifespan")
    if client_access_token_lifespan is not None or client_refresh_token_lifespan is not None:
        attributes = {}
        if client_access_token_lifespan is not None:
            attributes["access.token.lifespan"] = client_access_token_lifespan
        if client_refresh_token_lifespan is not None:
            attributes["refresh.token.lifespan"] = client_refresh_token_lifespan
        
        print(f" - Updating client token lifespans, access: {client_access_token_lifespan}, refresh: {client_refresh_token_lifespan}")
        update_client_settings_attributes(baseUrl, token, realm_name, client_id, attributes)

    if len(users) > 0:
        print(f"Creating users in realm '{realm_name}'...")
    else:
        print(f"No users to create in realm '{realm_name}'.")

    for user in users:
        username = user["username"]
        roles = user.get("roles", [])
        if not is_user_exists(baseUrl, token, realm_name, username):
            userId = create_user(
                baseUrl,
                token,
                realm_name,
                username,
                user.get("password", "password"),
                user.get("attributes", {}),
            )
            print(f"User '{username}' created in realm '{realm_name}'.")
            for role in roles:
                assign_role(baseUrl, token, realm_name, userId, role)
                print(f" - Assigned role '{role}' to user '{username}'.")
        else:
            print(f"User '{username}' already exists in realm '{realm_name}'.")

    print("Keycloak setup completed.")


def setup_env(keycloakSettings, envSettings):
    files = envSettings.get("files", {})
    env = envSettings.get("env", {})
    baseUrl = keycloakSettings["base_url"]
    realmName = keycloakSettings["realm_name"]
    clientId = keycloakSettings["client_id"]
    adminUsername = keycloakSettings["admin_user"]
    adminPassword = keycloakSettings["admin_pass"]
    adminToken = get_admin_token(baseUrl, adminUsername, adminPassword)

    for key, value in env.items():
        fileName = value
        file = files.get(fileName)
        if file is None:
            print(f"File '{key}' not exists, will create the file")
            json_create_empty_env(fileName)
        
        currentValue = json_env(fileName, key)
        if currentValue is not None:
            print(f"Env '{key}' already set to value '{currentValue}' in file '{fileName}', overriding...")
        
        if key == "client_secret":
            clientSecret = get_client_secret(baseUrl, adminToken, realmName, clientId)
            json_set_env(fileName, key, clientSecret)
            print(f" - Set client secret for client '{clientId}' in realm '{realmName}'")
        elif key == "signing_key":
            signingKey = get_signing_key(baseUrl, adminToken, realmName)
            json_set_env(fileName, key, signingKey)
            print(f" - Set signing key for realm '{realmName}'")
