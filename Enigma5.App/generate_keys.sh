openssl genrsa -aes256 -out private_key.pem $1
openssl rsa -in private_key.pem -outform PEM -pubout -out public_key.pem