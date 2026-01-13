#!/bin/bash

# Run Integration Tests with RabbitMQ in Docker
# This script starts RabbitMQ, runs integration tests, and cleans up

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.integration.yml"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}VsaResults Integration Tests${NC}"
echo -e "${GREEN}========================================${NC}"

# Function to cleanup
cleanup() {
    echo -e "\n${YELLOW}Cleaning up...${NC}"
    docker compose -f "${COMPOSE_FILE}" down -v --remove-orphans 2>/dev/null || true
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}Error: Docker is not running${NC}"
    exit 1
fi

# Start RabbitMQ
echo -e "\n${YELLOW}Starting RabbitMQ...${NC}"
docker compose -f "${COMPOSE_FILE}" up -d

# Wait for RabbitMQ to be healthy
echo -e "\n${YELLOW}Waiting for RabbitMQ to be ready...${NC}"
MAX_RETRIES=30
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if docker compose -f "${COMPOSE_FILE}" exec -T rabbitmq rabbitmq-diagnostics check_port_connectivity > /dev/null 2>&1; then
        echo -e "${GREEN}RabbitMQ is ready!${NC}"
        break
    fi

    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "Waiting... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo -e "${RED}Error: RabbitMQ failed to start${NC}"
    exit 1
fi

# Set environment variables for tests
export RABBITMQ_HOST=localhost
export RABBITMQ_PORT=5672
export RABBITMQ_USERNAME=guest
export RABBITMQ_PASSWORD=guest

# Run tests
echo -e "\n${YELLOW}Running integration tests...${NC}"
echo -e "${GREEN}========================================${NC}"

# Run all tests (InMemory + RabbitMQ)
if dotnet test "${SCRIPT_DIR}/tests/Tests.csproj" \
    --configuration Release \
    --no-restore \
    --verbosity normal \
    --collect:"XPlat Code Coverage" \
    --results-directory "${SCRIPT_DIR}/TestResults" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura; then

    echo -e "\n${GREEN}========================================${NC}"
    echo -e "${GREEN}All tests passed!${NC}"
    echo -e "${GREEN}========================================${NC}"

    # Show coverage summary if available
    if command -v reportgenerator &> /dev/null; then
        echo -e "\n${YELLOW}Generating coverage report...${NC}"
        reportgenerator \
            -reports:"${SCRIPT_DIR}/TestResults/**/coverage.cobertura.xml" \
            -targetdir:"${SCRIPT_DIR}/TestResults/CoverageReport" \
            -reporttypes:Html 2>/dev/null || true

        echo -e "${GREEN}Coverage report: ${SCRIPT_DIR}/TestResults/CoverageReport/index.html${NC}"
    fi
else
    echo -e "\n${RED}========================================${NC}"
    echo -e "${RED}Some tests failed!${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi

echo -e "\n${GREEN}Done!${NC}"
