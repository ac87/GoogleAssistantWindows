google-oauthlib-tool --scope https://www.googleapis.com/auth/assistant-sdk-prototype --headless --save --client-secrets Secrets\client_id.json

googlesamples-assistant-devicetool --client-secrets Secrets\client_id.json register-model --model "assistanttest-187121-myTest" --type LIGHT --manufacturer "Pieter De Rycke" --product-name "Assistant Test"

googlesamples-assistant-devicetool --client-secrets Secrets\client_id.json register-device --device "mylaptop" --model "assistanttest-187121-myTest" --nickname "My Laptop" --client-type SERVICE

googlesamples-assistant-devicetool --client-secrets Secrets\client_id.json list --model
googlesamples-assistant-devicetool --client-secrets Secrets\client_id.json list --device