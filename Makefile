COMPOSE = docker compose -p ori-chara-chronicle

NETWORK  = -f infra/compose/compose.network.yml
API      = -f infra/compose/compose.api.dev.yml
WEB      = -f infra/compose/compose.web.dev.yml
LOCAL    = -f infra/compose/compose.localstack.yml

.PHONY: dev api web local stop logs ps

dev:
	$(COMPOSE) $(NETWORK) $(LOCAL) $(API) $(WEB) up -d
	
api:
	$(COMPOSE) $(NETWORK) $(LOCAL) $(API) up -d

web:
	$(COMPOSE) $(WEB) up -d
	
local:
	$(COMPOSE) $(NETWORK) $(LOCAL) up -d

stop:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps