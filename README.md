# Upload Orchestrator API

API de orquestração de uploads de vídeos com suporte a upload multipart para Azure Blob Storage.

## Para que serve

Esta API gerencia o processo de upload de arquivos grandes (especialmente vídeos) permitindo:
- **Upload multipart**: Divisão de arquivos em partes para uploads mais eficientes e resilientes
- **URLs pré-assinadas**: Geração de URLs temporárias do Azure Blob Storage para upload direto
- **Controle de status**: Acompanhamento do processo de upload (Pending, Uploading, Completed, Failed)
- **Mensageria**: Publicação de eventos para processamento assíncrono via RabbitMQ
- **Autenticação JWT**: Endpoints protegidos com autenticação baseada em tokens

## Pré-requisitos

- **.NET 8.0 SDK**
- **Docker** e **Docker Compose** (para desenvolvimento local)
- **MongoDB** (banco de dados)
- **RabbitMQ** (mensageria)
- **Azure Blob Storage** ou **Azurite** (storage emulador local)

## Como desenvolver e colaborar

### Configuração inicial

1. Clone o repositório:
```bash
git clone <repository-url>
cd final-challenge-grupo-118-upload-orchestrator
```

2. Configure as variáveis de ambiente no `docker-compose.yml`:
   - `MongoDB__ConnectionString`
   - `AzureBlob__ConnectionString`
   - `AzureBlob__ContainerName`
   - `RabbitMQ__Uri`

3. Inicie os serviços com Docker Compose:
```bash
docker-compose up -d
```

### Executando localmente (sem Docker)

1. Restaure as dependências:
```bash
dotnet restore
```

2. Configure o `appsettings.Development.json` com suas credenciais

3. Execute a aplicação:
```bash
dotnet run --project src/UploadsApi.Api/UploadsApi.Api.csproj
```

### Testes

Execute os testes unitários:
```bash
dotnet test
```

### Estrutura do projeto

- **UploadsApi.Api**: Controllers e configuração da API
- **UploadsApi.Application**: Lógica de negócio e serviços
- **UploadsApi.Domain**: Entidades, enums e eventos de domínio
- **UploadsApi.Infrastructure**: Implementações de storage, mensageria e persistência

### Contribuindo

1. Crie uma branch para sua feature: `git checkout -b feature/nome-da-feature`
2. Faça commit das alterações: `git commit -m 'Adiciona nova feature'`
3. Push para a branch: `git push origin feature/nome-da-feature`
4. Abra um Pull Request

### Endpoints principais

- `POST /api/uploads` - Cria um novo upload
- `GET /api/uploads/{id}/presigned-urls` - Obtém URLs pré-assinadas para upload
- `POST /api/uploads/{id}/complete` - Finaliza o upload
- `GET /api/uploads/{id}` - Consulta status do upload
- `GET /api/uploads` - Lista uploads do usuário

