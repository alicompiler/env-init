from setup import setup_env, setup_keycloak
import json

if __name__ == "__main__":
    config = "dragon"
    envFilePath = f"./config/{config}/env.json"
    keycloakFilePath = f"./config/{config}/keycloak.json"

    with open(keycloakFilePath, "r") as f:
        keycloakSettings = json.load(f)
    with open(envFilePath, "r") as f:
        envSettings = json.load(f)

    print("Starting setup script...")
    
    setup_keycloak(keycloakSettings)
    setup_env(keycloakSettings, envSettings)
