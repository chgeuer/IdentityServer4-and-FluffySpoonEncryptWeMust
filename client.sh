#!/bin/bash

# https://identityserver4.readthedocs.io/en/latest/quickstarts/1_client_credentials.html

# curl --remote-name --time-cond cacert.pem "https://curl.haxx.se/ca/cacert.pem"
# echo quit | openssl s_client -showcerts -servername baldur.geuer-pollmann.de  -connect baldur.geuer-pollmann.de:443 | bat

service="http://beam-lan:6001"
sts_address="https://baldur.geuer-pollmann.de"

client_id="client"
client_secret="secret"

resource="${service}/showmemyidentity"

token_endpoint="$( curl \
    --silent \
    --cacert ./cacert.pem \
    --request GET \
    "${sts_address}/.well-known/openid-configuration" | \
        jq -r ".token_endpoint" )"
    
echo "${token_endpoint}"

access_token="$( curl \
    --silent \
    --request POST \
    --data-urlencode "grant_type=client_credentials" \
    --data-urlencode "client_id=${client_id}" \
    --data-urlencode "client_secret=${client_secret}" \
    --data-urlencode "resource=${resource}" \
    "${token_endpoint}" | \
        jq -r ".access_token" )"

function jwt_claims {
    local token="$1" ; 
    local base64Claims="$( echo "${token}" | awk '{ split($0,parts,"."); print parts[2] }' )" ;
    local length="$( expr length "${base64Claims}" )" ;

    local mod="$(( length % 4 ))" ;
    
    local base64ClaimsWithPadding
    if   [ "${mod}" = 0 ]; then base64ClaimsWithPadding="${base64Claims}"    ; 
    elif [ "${mod}" = 1 ]; then base64ClaimsWithPadding="${base64Claims}===" ; 
    elif [ "${mod}" = 2 ]; then base64ClaimsWithPadding="${base64Claims}=="  ; 
    elif [ "${mod}" = 3 ]; then base64ClaimsWithPadding="${base64Claims}="   ; fi

    echo "$( echo "${base64ClaimsWithPadding}" | base64 -d )"
}

function tweak_jwt_timestamps {
    local json="$1" ; 

    # function adapt() {  local key="$1" ; echo "\"$( date -d @$( echo "${json}" | jq ".${ key }" ) +%Y-%m-%d--%H-%M-%S )\"" ; } ;
    # local nbf="$( adapt "nbf" )" ; local exp="$( adapt "exp" )" ; local iat="$( adapt "iat" )" ;

    local nbf="\"$( date -d @$( echo "${json}" | jq ".nbf" ) +%Y-%m-%d--%H-%M-%S )\"" ;
    local exp="\"$( date -d @$( echo "${json}" | jq ".exp" ) +%Y-%m-%d--%H-%M-%S )\"" ;
    local iat="\"$( date -d @$( echo "${json}" | jq ".iat" ) +%Y-%m-%d--%H-%M-%S )\"" ;

    echo "${json}" | jq ". | .nbf=${nbf} | .exp=${exp} | .iat=${iat}"
}

 # jq "[.[] | select(.principalId == \"37f3d08b-1ad8-4975-9f74-1ab88d08717a\") | {principalName: .principalName, scope: .scope, roleDefinitionName: .roleDefinitionName}]"

json_claims="$( echo "$( jwt_claims "${access_token}" )" )"
json_claims="$( tweak_jwt_timestamps "${json_claims}" )"
echo $json_claims | jq "."

curl \
    --silent --insecure \
    --request GET \
    --header "Authorization: Bearer ${access_token}" \
    "${resource}" | jq  
