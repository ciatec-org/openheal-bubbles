# Session & Data Management

## Arquitetura

3 camadas de persistência:

```
RUNTIME (Memória - Volátil)
  ├─ SessionManager: Cache userId, companyId, matchId
  ├─ GameManager: Estado do jogo atual
  └─ ScoreManager: Pontuação em tempo real
       ↓
LOCAL (Dispositivo - Persistente)
  └─ PlayerPrefs: Configurações locais
       ↓
CLOUD (Multi-Dispositivo - Persistente)
  └─ Firebase: UserSettings, GameState, MatchHistory
```

## SessionManager

**Padrão**: `SingletonMonoBehaviour<SessionManager>` (namespace global)

**Propósito**: Cache em memória para evitar chamadas repetidas ao Firebase Auth.

### API (Helpers Estáticos)

```csharp
// User Data (populado no login)
string userId = SessionManager.GetUserId();
string email = SessionManager.GetUserEmail();
string companyId = SessionManager.GetCompanyId();

// Game Session (populado ao iniciar partida)
string gameId = SessionManager.GetCurrentGameId();
string matchId = SessionManager.GetCurrentMatchId();
string presetId = SessionManager.GetCurrentPresetId();
string levelId = SessionManager.GetCurrentLevelId();

// Runtime (acesso direto via Instance)
int landmarks = SessionManager.Instance.CurrentLandmarks;

// Validação
bool isValid = SessionManager.IsUserDataValid();
bool hasGame = SessionManager.IsGameSessionValid();
```

### Ciclo de Vida

```
Login → AuthManager.SignIn() → InitializeUserData() automático
  ↓
Start Game → SetGameSession(gameId, matchId, presetId, levelId)
  ↓
Gameplay → Acessa dados via helpers estáticos
  ↓
End Match → ClearGameSession()
  ↓
Logout → ClearAll()
```

### Quando Usar

✅ Acessar dados do usuário durante gameplay  
✅ Armazenar dados temporários da partida  
✅ Usar helpers estáticos para código limpo

❌ Persistir dados (use Firebase/PlayerPrefs)  
❌ Configurações (use UserSettingsService)

## Singleton Pattern

Base class no namespace global:

```csharp
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    
    public static T Instance { get => _instance; }
    public static bool HasInstance => _instance != null;
    
    protected virtual void Awake()
    {
        // Garante única instância + DontDestroyOnLoad
        // Move para root se necessário
    }
    
    protected virtual void OnInitialize() { /* Override para inicialização */ }
}
```

**Uso**:

```csharp
public class SessionManager : SingletonMonoBehaviour<SessionManager>
{
    protected override void OnInitialize()
    {
        // Inicialização aqui
    }
}
```

## Service Pattern

**Manager**: Lógica + Estado (SessionManager, AuthManager, BubblesGameManager, ScoreManager)  
**Service**: Comunicação + Persistência (UserSettingsService, GameStateService, GameSettingsService)

## Fluxo de Dados

**Login**:
```
LoginUI.OnLoginButton()
  → AuthManager.SignIn(email, password)
  → Firebase Auth
  → AuthManager chama SessionManager.InitializeUserData()
  → SessionManager busca dados de FirebaseAuth.CurrentUser
  → Navigate to MainMenu
```

**Gameplay**:
```
BubblesGameManager.Start()
  → SessionManager.GetUserId() (cached, sem Firebase calls)
  → SessionManager.SetGameSession(gameId, matchId, presetId, levelId)
  → Durante jogo: acessa via helpers estáticos
  → Fim: Salva no Firebase via Services
```

**Logout**:
```
Limpar sessão manualmente
  → SessionManager.ClearAll()
  → Navigate to Login scene
```

## Setup

**Cena Login**:
- GameObject "SessionManager" + componente `SessionManager.cs`
- GameObject "AuthManager" + componente `AuthManager.cs`
- `DontDestroyOnLoad` automático via `SingletonMonoBehaviour`

**Ordem de inicialização**:
```
FirebaseInitializer → AuthManager.OnInitialize()
  → Login → SessionManager.InitializeUserData()
  → Game Managers
```

**Imports**:
```csharp
// Classes estão no namespace global, sem using necessário
SessionManager.GetUserId();
AuthManager.Instance.SignIn(...);
```

## Comparação de Persistência

| Método | Performance | Persistente | Multi-Device | Uso |
|--------|-------------|-------------|--------------|-----|
| SessionManager | ⚡ | ❌ | ❌ | Cache de sessão |
| PlayerPrefs | ⚡ | ✅ Local | ❌ | Configs locais |
| Firebase | 🐌 | ✅ Cloud | ✅ | Dados usuário |
| ScriptableObject | ⚡ | ✅ Asset | ❌ | Configs estáticas |

## Nomenclatura

- `Manager`: Lógica + estado
- `Service`: Comunicação backend
- `Controller`: UI/Input
- `Data`: POCOs
- `Utils/Helper`: Funções auxiliares

## Best Practices

✅ Use helpers estáticos: `SessionManager.GetUserId()`  
✅ `InitializeUserData()` é chamado automaticamente após login  
✅ Valide com `SessionManager.IsUserDataValid()` antes de usar  
✅ Limpe com `ClearGameSession()` entre partidas e `ClearAll()` no logout  
✅ Estenda `SingletonMonoBehaviour<T>` para novos Managers

❌ Não acesse `FirebaseAuth.CurrentUser` repetidamente (use SessionManager)  
❌ Não reimplemente Singleton manualmente  
❌ Não armazene dados sensíveis (passwords, tokens)  
❌ Não chame `InitializeUserData()` manualmente (AuthManager já faz)

## DEPRECATED

**`GameSessionData`** → Use `SessionManager` (será removido)