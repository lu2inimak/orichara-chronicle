COMPOSE = docker compose -p ori-chara-chronicle

NETWORK  = -f infra/compose/compose.network.yml
API      = -f infra/compose/compose.api.dev.yml
WEB      = -f infra/compose/compose.web.dev.yml
LOCAL    = -f infra/compose/compose.localstack.yml

API_PORT ?= 8080
WEB_PORT ?= 3000
NEXT_PUBLIC_API_BASE_URL ?= http://localhost:$(API_PORT)

.PHONY: dev api web local stop logs ps rebuild

dev:
	API_PORT=$(API_PORT) WEB_PORT=$(WEB_PORT) \
	NEXT_PUBLIC_API_BASE_URL=$(NEXT_PUBLIC_API_BASE_URL) \
	$(COMPOSE) $(NETWORK) $(LOCAL) $(API) $(WEB) up -d
	
api:
	$(COMPOSE) $(NETWORK) $(LOCAL) $(API) up -d

web:
	$(COMPOSE) $(WEB) up -d

stop:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps

rebuild:
	$(COMPOSE) $(NETWORK) $(LOCAL) $(API) $(WEB) down
	API_PORT=$(API_PORT) WEB_PORT=$(WEB_PORT) \
	NEXT_PUBLIC_API_BASE_URL=$(NEXT_PUBLIC_API_BASE_URL) \
	$(COMPOSE) $(NETWORK) $(LOCAL) $(API) $(WEB) up -d --build
