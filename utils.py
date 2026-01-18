import time
import requests
from typing import Callable, Iterable, Optional


def wait_for_http_ready(
    check_urls: Iterable[str],
    timeout_seconds: int = 30,
    poll_interval: float = 1.0,
    is_success: Optional[Callable[[requests.Response], bool]] = None,
) -> bool:
    deadline = time.time() + timeout_seconds
    last_error = None
    if is_success is None:
        is_success = lambda r: r.status_code == 200  # noqa: E731

    while time.time() < deadline:
        for url in check_urls:
            try:
                r = requests.get(url, timeout=3)
                if is_success(r):
                    return True
            except Exception as e:  # network not ready yet
                last_error = e
        time.sleep(poll_interval)

    if last_error:
        print(
            f"Service readiness check timed out after {timeout_seconds}s. Last error: {last_error}"
        )
    else:
        print(f"Service readiness check timed out after {timeout_seconds}s.")
    return False


def wait_for_keycloak_ready(baseUrl, timeout_seconds: int = 30, poll_interval: float = 1.0) -> bool:
    check_url_candidates = [
        f"{baseUrl}/realms/master/.well-known/openid-configuration",
        f"{baseUrl}/realms/master",
        f"{baseUrl}/",
    ]
    return wait_for_http_ready(
        check_url_candidates,
        timeout_seconds=timeout_seconds,
        poll_interval=poll_interval,
    )
