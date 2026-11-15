"""Fresh CMS installation against a disposable published application."""
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

root = Path(sys.argv[1]).resolve()
assert (root / "Smartstore.Web.dll").is_file(), "Use a published application"
database = root / "App_Data" / "Tenants" / "Default" / "ommcms-smoke.db"
assert not database.exists(), "Smoke test needs a new database"
base = "http://127.0.0.1:5087"
env = dict(os.environ, ASPNETCORE_URLS=base, ASPNETCORE_ENVIRONMENT="Production",
           OmmCMS__PreviewShell="true")
log = root / "ommcms-smoke.log"
process = None


def request(path, data=None, timeout=30):
    payload = urllib.parse.urlencode(data).encode() if data is not None else None
    try:
        with urllib.request.urlopen(base + path, data=payload, timeout=timeout) as response:
            return response.status, response.read().decode(errors="replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode(errors="replace")


def start():
    global process
    with log.open("a", encoding="utf-8") as output:
        process = subprocess.Popen(["dotnet", "Smartstore.Web.dll"], cwd=root,
                                   env=env, stdout=output, stderr=subprocess.STDOUT)


def stop():
    global process
    if process is not None:
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=30)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=10)
        process = None


def wait_for(path, timeout=180):
    until = time.monotonic() + timeout
    while time.monotonic() < until:
        if process.poll() is not None:
            raise RuntimeError(f"Application exited: {process.returncode}")
        try:
            status, body = request(path, timeout=10)
            if status == 200:
                return body
        except (OSError, TimeoutError):
            pass
        time.sleep(2)
    raise RuntimeError(f"Application did not become ready: {path}")


try:
    start()
    page = wait_for("/install?noAutoInstall=true")
    assert "OmmCMS" in page, "CMS installer branding missing"
    assert 'name="InstallSampleData"' not in page, "Shop sample switch displayed"
    password = "Smoke-" + uuid.uuid4().hex + "!9"
    status, body = request("/install/Install", {
        "AdminEmail": "admin@ommcms.test",
        "AdminPassword": password,
        "ConfirmPassword": password,
        "DataProvider": "sqlite",
        "PrimaryLanguage": "en-US",
        "MediaStorage": "fs",
        "CreateDatabase": "true",
        "DbName": "ommcms-smoke",
        "InstallSampleData": "true",
    }, timeout=300)
    assert status == 200, f"Installer HTTP {status}"
    result = {key.lower(): value for key, value in json.loads(body).items()}
    assert result.get("success"), f"Installation failed: {result.get('errors')}"
    stop()
    assert database.is_file(), "SQLite database not found"
    with sqlite3.connect(database.as_uri() + "?mode=ro", uri=True) as connection:
        def rows(sql):
            return [row[0] for row in connection.execute(sql)]

        assert rows("SELECT Name FROM Store") == ["OmmCMS"]
        assert rows("SELECT COUNT(*) FROM Product") == [0]
        assert rows("SELECT COUNT(*) FROM Category") == [0]
        topics = set(rows("SELECT SystemName FROM Topic"))
        assert {"HomePageText", "AboutUs", "ContactUs"} <= topics
        assert not topics.intersection({"PaymentInfo", "ShippingInfo", "CheckoutAsGuestOrRegister"})
        assert "catalog" not in rows("SELECT ProviderName FROM MenuItemRecord")
        tasks = set(rows("SELECT Type FROM ScheduleTask"))
        assert "QueuedMessagesSendTask" in tasks
        assert not tasks.intersection({"ProductRuleEvaluatorTask", "UpdateExchangeRateTask"})
    print("CMS installer and SQLite starter content passed.")
    start()
    page = wait_for("/")
    assert "Welcome to OmmCMS" in page, "CMS home page missing"
    for path in ("/about", "/contactus", "/login"):
        status, _ = request(path)
        assert status == 200, f"{path}: expected HTTP 200, got {status}"
        print(f"{path}: {status}")
    for path in ("/cart", "/checkout", "/admin/product/list", "/odata/Products"):
        status, _ = request(path)
        assert status == 404, f"{path}: expected HTTP 404, got {status}"
        print(f"{path}: {status}")
    print("CMS fresh-install HTTP smoke test passed.")
except Exception:
    if log.exists():
        print(log.read_text(errors="replace")[-25000:], file=sys.stderr)
    raise
finally:
    stop()
