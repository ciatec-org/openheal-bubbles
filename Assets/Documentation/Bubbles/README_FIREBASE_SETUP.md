# Configuração de Segurança

## Arquivos Sensíveis (NÃO no repositório)

**`Assets/Firebase/`** (pasta completa)
- SDK do Firebase para Unity
- **Importação:** do arquivo local
- Importar o Auth, Database e Firestore
**IMPORTANTE:** mover as pastas `Firebase`, `Editor Default Resources` e `ExternalDependencyManager` para dentro de `Assets/ThirdyPart`

```
Assets/
├── ThirdPart/Editor Default Resources/
├── ThirdPart/ExternalDependencyManager/
└── ThirdPart/Firebase/
```


### Adicionar Arquivos de Configuração
   - Insira `google-services.json` em `Assets/google-services.json`
   - Insira `google-services-desktop.json` dentro de `Assets/StreamingAssets/google-services-desktop.json`

**IMPORTANTE:**
- Nunca commite esses arquivos no Git

## Estrutura Esperada configurada manualmente

```
Assets/
├── google-services.json               <- não no Git
├── StreamingAssets/
│   └── google-services-desktop.json   <- não no Git
├──ThirdPart/Editor Default Resources/
├── ThirdPart/ExternalDependencyManager/
└── ThirdPart/Firebase/
```

