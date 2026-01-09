@rem needs keytool -importcert -cacerts -storepass changeit -file {path}\ca.crt
npx openapi-generator-cli generate -i http://localhost:5227/swagger/v1/swagger.yaml -g typescript-angular -o .\backend --config openapi.config.json
pause

