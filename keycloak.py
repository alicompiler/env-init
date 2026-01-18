import requests
import json


def get_admin_token(baseUrl, username, password):
    url = f"{baseUrl}/realms/master/protocol/openid-connect/token"
    data = {
        "grant_type": "password",
        "client_id": "admin-cli",
        "username": username,
        "password": password
    }
    r = requests.post(url, data=data)
    r.raise_for_status()
    return r.json()["access_token"]


def create_realm(baseUrl, token, realmName):
    url = f"{baseUrl}/admin/realms"
    data = {
        "realm": realmName,
        "enabled": True
    }
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.post(url, json=data, headers=headers)
    r.raise_for_status()

def is_realm_exists(baseUrl, token, realmName):
    url = f"{baseUrl}/admin/realms/{realmName}"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    return r.status_code == 200


def create_client(baseUrl, token, realmName, clientId, options):
    url = f"{baseUrl}/admin/realms/{realmName}/clients"
    data = {
        "clientId": clientId,
        "enabled": True,
        "redirectUris": options.get("redirectUris", []),
        "webOrigins": options.get("webOrigins", []),
        "protocol": "openid-connect",
        "standardFlowEnabled": True,
        "publicClient": False,
        "serviceAccountsEnabled": options.get("enable_service_account", False)
    }
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.post(url, json=data, headers=headers)
    r.raise_for_status()

def is_client_exists(baseUrl, token, realmName, clientId):
    url = f"{baseUrl}/admin/realms/{realmName}/clients/{clientId}"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    return r.status_code == 200

def get_client_or_fail(baseUrl, token, realmName, clientId):
    url = f"{baseUrl}/admin/realms/{realmName}/clients/{clientId}"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    return r.json()


def create_user(baseUrl, token, realmName, user):
    url = f"{baseUrl}/admin/realms/{realmName}/users"
    
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    data = {
            "username": user.get("username"),
            "firstName": user.get("firstName"),
            "lastName": user.get("lastName"),
            "email": user.get("email"),
            "emailVerified":     user.get("emailVerified", True),
            "enabled": user.get("enabled", True),
            "credentials": [{"type": "password", "value": user.get("password"), "temporary": False}]
        }
    r = requests.post(url, json=data, headers=headers)
    r.raise_for_status()
    userId = r.headers.get("Location").split("/")[-1]
    return userId
    
        
def is_user_exists(baseUrl, token, realmName, username):
    url = f"{baseUrl}/admin/realms/{realmName}/users?username={username}"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    return r.status_code == 200

def get_all_users(baseUrl, token, realmName):
    url = f"{baseUrl}/admin/realms/{realmName}/users"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    return r.json()


def create_role(baseUrl, token, realmName, role_name):
    url = f"{baseUrl}/admin/realms/{realmName}/roles"
    data = {"name": role_name}
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.post(url, json=data, headers=headers)
    r.raise_for_status()

def is_role_exists(baseUrl, token, realmName, role_name):
    url = f"{baseUrl}/admin/realms/{realmName}/roles/{role_name}"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    return r.status_code == 200


def assign_role(baseUrl, token, realm, user_id, role):
    url = f"{baseUrl}/admin/realms/{realm}/users/{user_id}/role-mappings/realm"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    role_url = f"{baseUrl}/admin/realms/{realm}/roles/{role}"
    r = requests.get(role_url, headers=headers)
    r.raise_for_status()
    role = r.json()
    r = requests.post(url, json=[role], headers=headers)
    r.raise_for_status()

def list_assigned_roles(baseUrl, token, realm, user_id):
    url = f"{baseUrl}/admin/realms/{realm}/users/{user_id}/role-mappings/realm"
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    roles = r.json()
    return [{"id": role["id"], "name": role["name"]} for role in roles]
    client = get_client_or_fail(baseUrl, token, realm, client_id)
    client_id = client["id"]
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    if not client.get("serviceAccountsEnabled"):
        client["serviceAccountsEnabled"] = True
        r = requests.put(f"{url}/{client_id}", json=client, headers=headers)
        r.raise_for_status()

def assign_realm_management_roles_to_service_account(baseUrl, token, realm, client_id):
    client = get_client_or_fail(baseUrl, token, realm, client_id)
    client_id = client["id"]

    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    sa_url = f"{baseUrl}/admin/realms/{realm}/clients/{client_id}/service-account-user"
    r = requests.get(sa_url, headers=headers)
    r.raise_for_status()
    service_account_user = r.json()
    service_account_user_id = service_account_user["id"]

    roles_url = f"{baseUrl}/admin/realms/{realm}/clients"
    r = requests.get(roles_url, headers=headers, params={"clientId": "realm-management"})
    r.raise_for_status()
    realm_mgmt_clients = r.json()
    if not realm_mgmt_clients:
        raise Exception("Client 'realm-management' not found.")
    realm_mgmt_client = realm_mgmt_clients[0]
    realm_mgmt_client_id = realm_mgmt_client["id"]
    client_roles_url = f"{baseUrl}/admin/realms/{realm}/clients/{realm_mgmt_client_id}/roles"
    r = requests.get(client_roles_url, headers=headers)
    r.raise_for_status()
    all_roles = r.json()
    
    roles_to_assign = ["view-users", "manage-users", "query-users"]
    roles = [role for role in all_roles if role["name"] in roles_to_assign]
    user_roles_url = f"{baseUrl}/admin/realms/{realm}/users/{service_account_user_id}/role-mappings/clients/{realm_mgmt_client_id}"
    r = requests.post(user_roles_url, json=roles, headers=headers)
    r.raise_for_status()


def update_realm_settings(baseUrl, token, realm, key, value):
    realm_url = f"{baseUrl}/admin/realms/{realm}"
    update_payload = {
        key: value
    }

    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}"
    }

    configResponse = requests.get(realm_url, headers=headers)
    configResponse.raise_for_status()
    config = configResponse.json()
    config.update(update_payload)

    put_response = requests.put(realm_url, headers=headers, data=json.dumps(config))
    put_response.raise_for_status()

def update_realm_attribute_settings(baseUrl, token, realm, attributes):
    update_realm_settings(baseUrl, token, realm, "attributes", attributes)

def update_client_settings_attributes(baseUrl, token, realm, client_id, attributes):
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}"
    }

    client = get_client_or_fail(baseUrl, token, realm, client_id)
    client_id = client["id"]

    client_url = f"{baseUrl}/admin/realms/{realm}/clients/{client_id}"

    update_payload = {
        "attributes": attributes
    }

    get_response = requests.get(client_url, headers=headers)
    get_response.raise_for_status()
    current_config = get_response.json()
    current_config.update(update_payload)

    put_response = requests.put(client_url, headers=headers, data=json.dumps(current_config))
    put_response.raise_for_status()


def get_client_secret(baseUrl, token, realmName, clientId):
    client = get_client_or_fail(baseUrl, token, realmName, clientId)
    client_id = client["id"]
    url = f"{baseUrl}/admin/realms/{realmName}/clients/{client_id}/client-secret"
    headers = {"Authorization": f"Bearer {token}"}
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    return r.json().get("value")

def get_signing_key(baseUrl, token, realmName):
    url = f"{baseUrl}/admin/realms/{realmName}/keys"
    headers = {"Authorization": f"Bearer {token}"}
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    keys = r.json().get("keys", [])
    for key in keys:
        if key.get("use") == "SIG" and key.get("type") == "RSA":
            return key.get("publicKey")
    raise Exception("Signing key not found")

