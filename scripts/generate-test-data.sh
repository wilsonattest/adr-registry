#!/bin/bash
# Generate test ADR data for development testing

OUTPUT_PATH="${1:-test-repos}"

# Repository definitions
REPOS=("payment-service" "user-management" "inventory-system" "notification-hub" "analytics-platform")

# Topics per repo
declare -A TOPICS
TOPICS["payment-service"]="payment-gateway transaction-processing refund-handling currency-conversion fraud-detection pci-compliance payment-methods recurring-billing invoice-generation payment-reconciliation chargeback-handling tax-calculation"
TOPICS["user-management"]="authentication authorization user-profiles role-management password-policy session-handling oauth-integration sso user-preferences account-recovery audit-logging gdpr-compliance"
TOPICS["inventory-system"]="stock-tracking warehouse-management order-fulfillment supplier-integration inventory-forecasting barcode-scanning batch-management returns-processing stock-alerts multi-location inventory-valuation cycle-counting"
TOPICS["notification-hub"]="email-delivery push-notifications sms-gateway in-app-messaging notification-preferences template-management delivery-tracking rate-limiting priority-queuing batch-notifications webhook-delivery notification-history"
TOPICS["analytics-platform"]="data-ingestion event-tracking dashboard-design report-generation data-retention real-time-analytics custom-metrics data-export visualization-library query-optimization data-warehouse etl-pipeline"

# Technologies
TECHNOLOGIES=("PostgreSQL" "Redis" "Kafka" "RabbitMQ" "Elasticsearch" "MongoDB" "GraphQL" "REST-API" "gRPC" "WebSockets" "Docker" "Kubernetes" "AWS-Lambda" "Azure-Functions" "Terraform" "GitHub-Actions" "React" "Vue.js" "TypeScript" "Node.js" "DotNet-Core" "Python" "OAuth-2.0" "JWT" "OpenID-Connect" "HashiCorp-Vault")

# Patterns
PATTERNS=("CQRS-pattern" "Event-Sourcing" "Saga-pattern" "Circuit-Breaker" "Retry-policy" "Bulkhead-pattern" "Cache-Aside" "Outbox-pattern" "Strangler-Fig" "Anti-Corruption-Layer" "Backend-for-Frontend" "API-Gateway")

# Decision verbs
VERBS=("Use" "Adopt" "Implement" "Switch-to" "Integrate" "Migrate-to" "Standardize-on" "Choose" "Deploy")

# Deciders
DECIDERS=("Alice Chen" "Bob Smith" "Carol Johnson" "David Kim" "Eva Martinez" "Frank Wilson" "Grace Lee" "Henry Brown" "Iris Taylor" "Jack Anderson")

# Statuses with weights (approximate)
get_random_status() {
    local r=$((RANDOM % 100))
    if [ $r -lt 60 ]; then echo "Accepted"
    elif [ $r -lt 80 ]; then echo "Proposed"
    elif [ $r -lt 95 ]; then echo "Superseded"
    else echo "Deprecated"
    fi
}

# Get random element from array
get_random() {
    local arr=("$@")
    echo "${arr[$((RANDOM % ${#arr[@]}))]}"
}

# Get random date in last 3 years
get_random_date() {
    local days_ago=$((RANDOM % 1095))
    date -d "-$days_ago days" +%Y-%m-%d 2>/dev/null || date -v-${days_ago}d +%Y-%m-%d
}

# Generate ADR content
generate_adr() {
    local number=$1
    local title=$2
    local status=$3
    local date=$4
    local deciders=$5
    local topic=$6

    local tech=$(get_random "${TECHNOLOGIES[@]}")
    local pattern=$(get_random "${PATTERNS[@]}")

    cat << EOF
# [ADR-$(printf "%04d" $number)] $title

## Metadata

| Field       | Value                    |
|-------------|--------------------------|
| Date        | $date |
| Status      | $status |
| Deciders    | $deciders |

## Context

Our $topic implementation needs to handle increasing scale and complexity. The current approach is becoming difficult to maintain and doesn't meet our performance requirements.

Key considerations include:
- Performance requirements for high-volume scenarios
- Team familiarity with the technology
- Long-term maintenance and support
- Integration with existing systems
- Cost implications

## Decision

We will use $tech for our $topic needs. This technology provides the features we need and aligns with our team's expertise.

Key aspects of this decision:
1. We will start with a proof of concept to validate the approach
2. Documentation will be created for the new implementation
3. Training will be provided for team members as needed
4. Monitoring and alerting will be set up from the beginning

## Consequences

### Positive

- Improved scalability for $topic
- Better alignment with industry best practices
- Reduced operational complexity
- Enhanced developer experience
- Better observability and debugging capabilities

### Negative

- Initial learning curve for the team
- Migration effort required for existing functionality
- Potential temporary increase in complexity during transition

### Neutral

- Will require updates to our CI/CD pipeline
- May influence future architectural decisions

## Alternatives Considered

### Alternative 1: Continue with current approach

We considered continuing with our existing implementation. This was rejected because it does not address the scalability concerns.

### Alternative 2: Use $pattern

We evaluated using $pattern. This was not chosen because it added unnecessary complexity for our use case.

## References

- Internal architecture guidelines
- Technology radar evaluation
EOF
}

echo "Generating test ADR data..."
echo "Output path: $OUTPUT_PATH"
echo ""

rm -rf "$OUTPUT_PATH"
mkdir -p "$OUTPUT_PATH"

total_adrs=0

for repo in "${REPOS[@]}"; do
    repo_path="$OUTPUT_PATH/$repo/docs/adr"
    mkdir -p "$repo_path"

    # Random count between 50 and 100
    adr_count=$((50 + RANDOM % 51))

    echo "Creating $repo with $adr_count ADRs..."

    # Get topics for this repo
    IFS=' ' read -ra repo_topics <<< "${TOPICS[$repo]}"

    for ((i=1; i<=adr_count; i++)); do
        topic=$(get_random "${repo_topics[@]}")
        verb=$(get_random "${VERBS[@]}")
        tech=$(get_random "${TECHNOLOGIES[@]}")

        # Generate title
        title="$verb $tech for $topic"

        # Get random status and date
        status=$(get_random_status)
        adr_date=$(get_random_date)

        # Get 2-3 random deciders
        num_deciders=$((2 + RANDOM % 2))
        decider_list=""
        for ((d=0; d<num_deciders; d++)); do
            if [ $d -gt 0 ]; then decider_list+=", "; fi
            decider_list+=$(get_random "${DECIDERS[@]}")
        done

        # Generate filename
        slug=$(echo "$title" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9 -]//g' | sed 's/ /-/g' | cut -c1-50)
        filename=$(printf "%04d-%s.md" $i "$slug")

        # Write ADR
        generate_adr "$i" "$title" "$status" "$adr_date" "$decider_list" "$topic" > "$repo_path/$filename"

        ((total_adrs++))
    done

    echo "  Created $adr_count ADRs in $repo"
done

echo ""
echo "Done! Generated $total_adrs ADRs across ${#REPOS[@]} repositories."
echo ""
echo "Test repositories created:"
for repo in "${REPOS[@]}"; do
    count=$(ls -1 "$OUTPUT_PATH/$repo/docs/adr/"*.md 2>/dev/null | wc -l)
    echo "  - $repo: $count ADRs"
done
