#!/bin/bash

# https://identityserver4.readthedocs.io/en/latest/quickstarts/1_client_credentials.html


service="https://beam-lan:6001"
sts_address="https://baldur.geuer-pollmann.de:443"

client_id="client"
client_secret="secret"

resource="${service}/identity"

token_endpoint="$( curl \
    --silent \
    --insecure \
    --request GET \
    "${sts_address}/.well-known/openid-configuration" | \
        jq -r ".token_endpoint")"
    
echo "${token_endpoint}"

access_token="$(curl \
    --silent \
    --request POST \
    --data-urlencode "grant_type=client_credentials" \
    --data-urlencode "client_id=${client_id}" \
    --data-urlencode "client_secret=${client_secret}" \
    --data-urlencode "resource=${resource}" \
    "${token_endpoint}" | \
        jq -r ".access_token")"

echo "${access_token}" | awk '{split($0,parts,"."); print parts[2]}' | base64 -d | jq

curl \
    --silent --insecure \
    --request GET \
    --header "Authorization: Bearer ${access_token}" \
    "${resource}" | jq
