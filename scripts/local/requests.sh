#!/bin/bash

BASE_URL_5134="http://localhost:5134"
BASE_URL_5135="http://localhost:5135"
BASE_URL_5136="http://localhost:5136"

echo "=== Buzzard Local Test ==="

echo "=== Expects: 200"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5136/"

echo "=== Expects: 502"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/"


echo "=== Expects: 403 - caused by blocked path"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/admin"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/ADMIN/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/.env"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/.git"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/.git/1234"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5134/private/test"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/admin"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/ADMIN/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/.env"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/private/test"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/.git"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" "$BASE_URL_5135/.git/1234"

echo "=== Expects: 403 - caused by blocked user-agent"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: barbad" "$BASE_URL_5134/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: evil/1.0" "$BASE_URL_5134/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: foobotbar" "$BASE_URL_5134/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: foo BOT bar" "$BASE_URL_5134/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: barbad" "$BASE_URL_5135/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: evil/1.0" "$BASE_URL_5135/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: foobotbar" "$BASE_URL_5135/"
curl -o /dev/null -s -w "%{http_code} %{url_effective}\n" -H "User-Agent: foo BOT bar" "$BASE_URL_5135/"

