set -euo pipefail
export DEBIAN_FRONTEND=noninteractive
apt-get -o Acquire::Retries=2 -o Acquire::http::Timeout=30 update
apt-get install --no-remove --no-install-recommends -y g++
c++ --version
dpkg-query -W g++ g++-11 libstdc++-11-dev
