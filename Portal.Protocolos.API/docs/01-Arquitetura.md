# Arquitetura

## Objetivo

Definir a arquitetura oficial do Portal.Protocolos.

Este documento estabelece os padrões técnicos que devem ser seguidos durante todo o desenvolvimento.

---

# Arquitetura em Camadas

Frontend

↓

Controllers

↓

Services

├── Repositories

├── Integrações

└── Policy Engine

↓

Banco de Dados

API Externa

---

# Responsabilidade das Camadas

## Controllers

Recebem requisições HTTP.

Não possuem regra de negócio.

Chamam apenas Services.

---

## Services

Contêm toda a regra de negócio.

Podem utilizar:

- Repository
- Integrações
- Policies

---

## Repository

Responsável apenas por acesso ao banco.

Permitido:

- SELECT
- INSERT
- UPDATE
- DELETE

Não deve conter regra de negócio.

---

## Integrações

Responsáveis pela comunicação com sistemas externos.

Nenhuma outra camada pode consumir APIs diretamente.

---

## Policy Engine

Responsável por determinar se um serviço pode ou não ser aberto.

Cada serviço possui sua própria Policy.

Exemplo:

DefesaPolicy

CondutorPolicy

JariPolicy

CetranPolicy

RessarcimentoPolicy

As Policies:

- não acessam banco
- não consomem APIs
- não lançam exceções
- recebem um ContextoServico
- retornam ResultadoPolicy

---

# Estrutura dos Models

Models

├── Seguranca

├── Cadastros

├── Atendimento

├── Integracao

└── Compartilhado

Cada módulo deve possuir:

Entity

Request

Response

---

# Estrutura dos Services

Services

├── Seguranca

├── Atendimento

├── Integracao

└── Cadastros

---

# Estrutura dos Repositories

Repositories

├── Seguranca

├── Atendimento

└── Cadastros

---

# Fluxo de uma Requisição

Controller

↓

Service

↓

Repository / Integração

↓

Policy (quando necessário)

↓

Response

---

# Regras Gerais

- Uma responsabilidade por classe.
- Repository não contém regra de negócio.
- Toda regra de negócio pertence ao Service ou às Policies.
- Integrações devem permanecer isoladas.
- Toda nova funcionalidade deve possuir documentação.
- Toda implementação deve ser validada antes do commit.

---

Versão: 0.1.0

Última atualização: 07/08/2026
