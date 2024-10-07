openssl genrsa -aes256 -out private-key.pem $1
openssl rsa -in private-key.pem -outform PEM -pubout -out public-key.pem