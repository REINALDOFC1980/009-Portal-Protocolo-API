# Portal.Protocolos

## Objetivo

O Portal.Protocolos é um sistema desenvolvido para permitir que cidadãos realizem a abertura e o acompanhamento de processos administrativos de trânsito de forma totalmente online.

O sistema tem como objetivo reduzir o atendimento presencial, automatizar validações e permitir que o usuário acompanhe todo o ciclo do seu requerimento.

---

# Como funciona

O usuário realiza o login utilizando CPF ou CNPJ.

Após a autenticação, o sistema consulta a base de infrações através de uma API externa e apresenta todos os Autos de Infração vinculados ao usuário.

Ao selecionar um Auto, o Portal avalia quais serviços podem ser realizados.

Todos os serviços são exibidos.

Os serviços indisponíveis permanecem visíveis, porém desabilitados, informando ao usuário o motivo da indisponibilidade.

Após selecionar um serviço disponível, o usuário preenche as informações necessárias, anexa os documentos obrigatórios e conclui a abertura do protocolo.

---

# Fluxo Principal

Login

↓

Consulta dos Autos

↓

Seleção do Auto

↓

Validação dos Serviços Disponíveis

↓

Preenchimento do Requerimento

↓

Anexação de Documentos

↓

Confirmação

↓

Geração do Protocolo

↓

Acompanhamento

---

# Tecnologias

Backend

- .NET 8
- ASP.NET Core Web API
- Dapper
- SQL Server
- JWT
- FluentValidation

Frontend

- React
- TypeScript
- TanStack Router
- Tailwind CSS
- shadcn/ui

---

# Arquitetura

Frontend

↓

API

↓

Banco Portal

+

API Externa de Infrações

---

# Filosofia do Projeto

O Portal.Protocolos não é um sistema de cadastro.

O Portal.Protocolos é um orquestrador de processos administrativos.

Grande parte das informações são obtidas através de integrações com sistemas externos.

O banco de dados do Portal armazena apenas as informações necessárias para controlar os protocolos, usuários e documentos.

---

Versão: 0.1.0

Última atualização: 07/08/2026
