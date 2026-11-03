# Local JWT configuration

`Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpirationMinutes` have non-secret defaults in the API configuration. Set a unique, unpredictable signing key of at least 32 UTF-8 bytes in the API project's .NET User Secrets. The following command generates a random key and passes it to Secret Manager without printing it or putting it in shell history:

```sh
python3 - <<'PY'
import json, secrets, subprocess
subprocess.run(['dotnet', 'user-secrets', 'set', '--project', 'Revivo.Api/Revivo.Api.csproj'],
               input=json.dumps({'Jwt:SigningKey': secrets.token_urlsafe(48)}), text=True, check=True)
PY
```

User Secrets are for local development and are not encrypted. Use a secure secret provider in deployed environments. Restart the API after setting the key. Configure the existing `ConnectionStrings:RevivoDb` secret separately.
