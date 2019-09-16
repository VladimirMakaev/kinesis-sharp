
FROM localstack/localstack
RUN apk add jq
COPY setup.sh /docker-entrypoint-initaws.d/
COPY waitForActive.sh /scripts/
RUN chmod +x /scripts/waitForActive.sh