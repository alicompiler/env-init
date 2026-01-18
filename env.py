import json

def is_env_file_exists(filePath):
    try:
        with open(filePath, "r"):
            return True
    except FileNotFoundError:
        return False

def json_env(filePath, key):
    with open(filePath, "r") as f:
        data = json.load(f)
        return data.get(key)

def json_create_empty_env(filePath):
    with open(filePath, "w") as f:
        json.dump({}, f)

def json_set_env(filePath, key, value):
    data = {}
    try:
        with open(filePath, "r") as f:
            data = json.load(f)
    except FileNotFoundError:
        pass

    data[key] = value

    with open(filePath, "w") as f:
        json.dump(data, f, indent=4)
