#!/bin/bash
set -euo pipefail
chown -R app:app /data /app/wwwroot/uploads
exec runuser -u app -- dotnet EmployeeTrainer.dll "$@"
