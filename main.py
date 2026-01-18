from setup import setup_env, setup_keycloak
import json

if __name__ == "__main__":
    envFilePath = "./config/env.json"
    keycloakFilePath = "./config/keycloak.json"

    with open(keycloakFilePath, "r") as f:
        keycloakSettings = json.load(f)
    with open(envFilePath, "r") as f:
        envSettings = json.load(f)

    setup_keycloak(keycloakSettings)
    setup_env(keycloakSettings, envSettings)
