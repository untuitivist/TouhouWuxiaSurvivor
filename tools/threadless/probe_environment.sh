set -eu
printf 'PATH=%s\n' "$PATH"
cat /etc/os-release
id
command -v python3 || true
command -v git || true
command -v gcc || true
command -v scons || true
command -v cmake || true
command -v dotnet || true
command -v curl || true
ls /usr/bin/python* /usr/bin/gcc* /usr/bin/curl /usr/bin/git 2>/dev/null || true
df -h / /mnt/u
free -h
