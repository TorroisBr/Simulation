# Simulation Architecture

> Constituição conceitual do projeto `TorroisBr/Simulation`.
>
> Este documento registra decisões arquiteturais de longa duração: o que os conceitos **significam**, quais distinções não podem ser colapsadas e quais direções futuras devem permanecer possíveis.
>
> Ele **não** substitui os documentos de estado de fase (`docs/PHASE5_STATE.md`, `docs/PHASE6_STATE.md`, etc.). Os documentos de fase dizem **o que está implementado agora**; este documento diz **o que deve continuar semanticamente verdadeiro**.

---

## 0. Como interpretar este documento

Os termos abaixo indicam o grau de compromisso:

- **DECIDIDO** — princípio ou semântica já acordada. Nova implementação deve preservá-lo salvo revisão arquitetural explícita.
- **DIREÇÃO** — direção futura aceita, mas cuja forma concreta ainda pode mudar depois de auditoria do código.
- **DEFERIDO** — ideia válida, mas que não deve ser implementada antecipadamente sem consumidor real.
- **ABERTO** — questão ainda não decidida. Não inferir automaticamente uma resposta.

Quando uma implementação atual for mais estreita que uma direção futura, **não generalizar só para “preparar o futuro”**. Preservar o espaço conceitual e generalizar quando existir uso real.

### Autoridade documental

1. `SIMULATION_ARCHITECTURE.md` — invariantes conceituais de longo prazo.
2. `PHASE*_STATE.md` — estado implementado/canonical da fase atual.
3. `AGENTS.md` — regras operacionais para agentes/Codex.
4. Código e testes canonical — contrato executável atual.

Uma fase pode implementar apenas um subconjunto da arquitetura planejada. Isso não torna o restante automaticamente implementado.

---

# Parte I — Visão do produto

## 1. O que este projeto é

**DECIDIDO**

O projeto é uma **plataforma de simulação de mundo persistente para RPG de mesa**, e não apenas um simulador de NPCs em Unity.

O objetivo de longo prazo é algo próximo de:

> “Dwarf Fortress para o estado de um mundo de campanha, mas controlável pelo GM.”

A emergência desejada não vem de simular tudo em detalhe, mas de fazer com que **acontecimentos relevantes produzam consequências coerentes através de domínios persistentes**.

Exemplo:

```text
seca
→ produção cai
→ falta alimento
→ preços sobem
→ mercadores mudam rotas
→ população migra
→ arrecadação cai
→ governo atrasa pagamentos
→ segurança piora
→ oposição política ganha força
```

Nenhuma dessas setas deve significar “sempre acontece”. Cada domínio pode interromper, amortecer ou redirecionar a cadeia.

---

## 2. Unity é Simulation Studio, não a autoridade conceitual

**DIREÇÃO**

Unity permanece extremamente importante como:

- autoria visual;
- editor de conteúdo;
- Observer/GM UI;
- mapas, grafos e timeline;
- operação local da simulação;
- host possível do Core.

Mas regras do mundo não devem depender conceitualmente de:

- `MonoBehaviour`;
- `ScriptableObject` como estado diário;
- `UnityEngine.Object` como identidade semântica;
- `Mathf`/Unity APIs quando o Core puder ser puro.

A direção de plataforma é:

```text
GM / USER
   │
   ├── Unity Simulation Studio
   ├── Foundry / Obsidian / Sites
   ├── CLI / Plugins
   └── APIs / outros clientes
              │
              ▼
       Simulation Host
       Unity ou .NET
              │
              ▼
        SIMULATION.CORE
           C# puro
              │
      ┌───────┴────────┐
      ▼                ▼
Persistence       Events / History /
Save / DB          Diagnostics / Diff
```

**Não existem duas simulações.** Todos os clientes devem operar sobre o mesmo modelo de domínio.

`ScriptableObject` continua excelente como **fonte de authoring**. No futuro, um asset Unity pode produzir a mesma definição pura que JSON ou outro formato produziria.

---

# Parte II — Constituição semântica

## 3. Cadeia semântica principal

**DECIDIDO**

A cadeia central do projeto é:

```text
WORLD TRUTH
     ↓
KNOWLEDGE
     ↓
INTERPRETATION / EVALUATION
     ↓
DECISION
     ↓
ACTION / PLAN / INTENT
     ↓
EXECUTION CONTEXT
     ↓
DOMAIN OUTCOME
     ↓
DOMAIN EVENT
     ↓
HISTORY / STATS / UI
```

Nem toda feature precisa materializar todas as camadas, mas não deve colapsá-las quando a distinção muda o comportamento.

---

## 4. Invariantes fundamentais

**DECIDIDO**

Estas distinções são constitucionais:

```text
WORLD TRUTH != KNOWLEDGE
WORLD TRUTH != INSTITUTIONAL RECOGNITION
KNOWLEDGE != MEMORY
KNOWLEDGE != INTERPRETATION
APPRAISAL != REACTION != RELATIONSHIP
REACTION != SUPPORT != POLITICAL POSITION
TARGET != PERCEIVED ATTRIBUTION
NORM != INTERPRETATION != ENFORCEMENT
AUTHORITY != INFLUENCE
AFFILIATION != SUPPORT != OBEDIENCE
COLLECTIVE POSITION != SUM OF MEMBER POSITIONS
POLITICAL SELECTION != ASSUMPTION OF OFFICE
IDENTITY != CLAIMED CONTINUITY
OWNERSHIP != CUSTODY != CONTROL
OWNERSHIP != JURISDICTION != CONTROL != ALLEGIANCE
RESIDENCE != PRESENCE
SIMULATION LOCATION != RENDERING COORDINATE
HEX COORDINATE != HEX ID != RENDERING COORDINATE
CONTAINMENT != SAME HEX != CONNECTIVITY
HEX ADJACENCY != TRAVERSABILITY
GEOMETRIC ADJACENCY != PHYSICAL PASSAGE != ACTOR CAPABILITY != PERMISSION != PERCEIVED PASSAGE
MOVING ENTITY != LOCATION
IN TRANSIT != NO FACTUAL POSITION
DISTANCE != TRAVEL TIME
HEX DISTANCE != PHYSICAL DISTANCE != TRAVEL TIME
SHORTEST PATH != FASTEST PATH != SAFEST PATH
ROUTE PLANNING USES KNOWLEDGE
TRAVERSAL EXECUTION REVALIDATES WORLD TRUTH
ROUTE ESTIMATE != EXECUTION TRUTH / ACTUAL CONTEXTUAL COST
PLAN STALE IN TRUTH != ACTOR KNOWS WHY
BATTLE != BATTLE LOCATION != BATTLE AFTERMATH
BATTLE ENDED != BATTLEFIELD MATERIAL STATE ENDED
BATTLEFIELD MATERIAL STATE != HISTORICAL MEMORY
CONTENT DEFINITION != CURRENT SPATIAL WORLD STATE
MEMBERSHIP != RESIDENCE != CITIZENSHIP != ALLEGIANCE
GENEALOGY != HOUSEHOLD != DYNASTY
OFFICE != TITLE != SOCIAL STATUS
HOSTILITY != CONFLICT
CONFLICT != WAR
WAR != BATTLE
BATTLE RESULT != WAR RESULT
BATTLE != TERRITORIAL CONTROL
COMMAND != LOYALTY != ALLEGIANCE
MANPOWER SOURCE != COMMAND
SUPPLY != FUNDING / PAY
EVENT != MEMORY != HISTORY
DECISION != EXECUTION
EVENT != TRUTH
PERSON != NPCRUNTIME
DEFINITION ID != RUNTIME / SEMANTIC INSTANCE ID
GENERATED BACKSTORY != SIMULATED HISTORY
INITIAL GENERATED STATE != IMMUTABLE WORLD
INITIAL GENERATION != RUNTIME MUTATION AUTHORITY
RUNTIME-CREATED WORLD STRUCTURE = NORMAL WORLD TRUTH
PLAYER DECISION AUTHORITY != OMNISCIENT INFORMATION AUTHORITY
IN-WORLD DIVINITY != GM / EXTERNAL AUTHORITY
```

Essas desigualdades significam **conceitos diferentes**, não necessariamente classes diferentes.

---

## 5. World Truth != Knowledge

**DECIDIDO**

O mundo possui estado factual autoritativo. Um ator toma decisões com o que sabe, não com acesso secreto à verdade.

Exemplo:

```text
WORLD TRUTH:
Rei morreu no dia 100.

Conselho:
soube no dia 101.

Guilda:
soube no dia 104.

General distante:
no dia 106 ainda acredita que o rei vive.
```

O general pode agir coerentemente com informação velha. Na execução, as precondições factuais necessárias são revalidadas.

### Consequência arquitetural

- Planning/decision usa `Knowledge`.
- Execution valida `World Truth`.
- Conhecimento não deve se sincronizar magicamente com a verdade.
- Ler a verdade escondida durante planning é bug arquitetural.

---

## 6. Querying → deciding → mutating → recording

**DECIDIDO**

A ordem conceitual é:

```text
querying
→ deciding
→ mutating
→ recording
```

Regras:

- avaliação não muta;
- preview não muta;
- geração de candidatos não avança contadores, não muda planos e não redireciona intenções;
- recording não altera domínio;
- recording não consome RNG;
- query, preview e diagnostics não consomem aleatoriedade autoritativa nem alteram o resultado da simulação;
- execução revalida estado e stale guards;
- mutações compostas que precisam ser atômicas devem validar antes de aplicar.

---

# Parte III — Identidade, definições e níveis de representação

## 7. Definition vs runtime state

**DECIDIDO**

Definições estáticas descrevem tipos e configuração inicial. Runtime descreve o mundo mutável.

Exemplos:

| Definição / authoring | Runtime |
|---|---|
| tipo de job | profissão/emprego atual |
| tipo de ação | executor, alvo, resultado |
| tipo de organização | organização existente |
| definição de cargo | ocupante e mandato |
| regra de lei | regra vigente e decisões jurídicas |
| tipo de doença | caso ativo e gravidade |
| tipo de propriedade | ativo existente e titular |
| regra de sucessão | disputa/candidatos atuais |
| estrada inicial | estado/manutenção/pedágio atual |

Não alterar assets para representar mudanças do mundo.

---

## 8. Identidade semântica deve sobreviver ao tempo

**DECIDIDO**

Uma instância do mundo precisa de identidade própria, distinta da definição que a originou.

Regras:

- IDs de definição não substituem IDs de instância;
- identidade permanece após morte, vacância ou dormência;
- referências persistíveis usam IDs semânticos;
- quando uma relação individual precisa sobreviver à materialização, ao
  descarregamento ou ao save/load, ela usa a identidade persistente apropriada,
  normalmente `PersonId`, e não um identificador de representação como
  `NpcRuntimeId`;
- `NpcRuntimeId` pode servir para execução transitória, lookup runtime,
  projeção carregada ou estado local de um agente, mas não deve definir
  identidade histórica persistente;
- caches/índices podem ser reconstruídos;
- display name não é identidade;
- `ToString()`, hash de referência e InstanceID Unity não são identidade.

---

## 9. Existência, representação e processamento

**DECIDIDO**

Existência individual, representação rica e intensidade de processamento são
dimensões diferentes:

```text
Population aggregate
        ↓
Person
        ├── Person-only / não materializada
        └── estado individual rico
              ├── carregado
              └── serializado/descarregado (direção futura)

estado individual rico
        ├── Active
        └── Dormant
```

### Significado

- **Population aggregate** — quantidade abstrata para população comum. Não é
  uma categoria para a qual uma Person individualizada retorna por perder
  relevância.
- **Person** — identidade individual factual, capaz de sobreviver
  historicamente e de participar de outros stores e relações por `PersonId`.
  O conceito não é exclusivo de humanos: um indivíduo não humano cuja
  identidade factual importa pode ser uma Person, sem criar uma segunda
  simulação de indivíduos. Espécie e biologia não substituem identidade nem
  confundem limitações físicas com regras sociais; seus contratos concretos
  dependem de consumidores futuros.
- **Person-only / Person não materializada** — a identidade individual existe,
  mas não há estado individual rico persistido. Isso não é `Dormant`.
- **Estado individual rico** — representação que preserva comportamento,
  conhecimento, planos, compromissos e outras informações individuais que
  alteram decisões futuras. Pode estar carregada ou, em direção futura,
  serializada e descarregada.
- **Active / Dormant** — intensidade ou política de processamento do estado
  individual rico. Não são espécies diferentes de indivíduo nem substituem
  estados de vida, presença, viagem ou autoridade.

No código atual, um estado individual rico carregado é representado
principalmente por `NpcRuntime`. Isso é uma representação runtime vigente, não
uma definição de que `NpcRuntime` será a forma conceitual eterna desse estado
quando existir um futuro `Simulation.Core` ou persistência fora da memória.

Uma Person não materializada ainda pode:

- viver ou morrer;
- possuir residência;
- possuir propriedade;
- participar de genealogia;
- ocupar ou disputar cargos;
- possuir afiliações e relações por `PersonId`;
- receber consequências compatíveis com sua representação.

### Active

`Active` significa que o indivíduo com estado rico está elegível para
autonomia e processamento de maior frequência ou fidelidade. Não significa
necessariamente avaliação completa diária, nem presença física em uma área.
Viagem, ocultação, residência, expedição e outros estados espaciais ou
operacionais permanecem conceitos separados.

### Dormant

`Dormant` preserva o estado individual rico, mas reduz o processamento
autônomo de rotina. Um indivíduo dormente:

- continua existindo no mundo;
- não é reabsorvido na população agregada;
- não tem suas obrigações ou seu tempo congelados;
- pode continuar sujeito a eventos, vencimentos e processos temporais próprios;
- não deixa de possuir conhecimento, planos, relações, propriedade ou cargo
  apenas por receber menos processamento autônomo.

`Person-only` não significa baixa frequência de processamento. Significa
ausência de estado individual rico persistido.

### Regras de representação

- materializar ou desmaterializar uma representação rica não altera a
  população factual;
- `Active → Dormant`, `Dormant → Active`, carregado → descarregado e
  descarregado → carregado não adicionam nem removem população;
- `NpcRuntime → aggregate` não é uma transição normal de um indivíduo
  individualizado;
- materialização não deve transformar automaticamente uma Person em `Active`;
- população grande não exige estado rico carregado para todas as Persons;
- setting pode individualizar muitas/all Persons sem torná-las todas agentes
  ativos;
- disponibilidade de CPU, pressão de memória ou estado de uma tela não são
  autoridade para decidir uma mudança semântica do mundo.

---

## 10. Regra de abstração

**DECIDIDO**

Representar explicitamente aquilo cuja **identidade, distribuição, persistência ou conhecimento desigual** muda decisões futuras. Usar parâmetros/agregados quando o efeito agregado preserva as decisões relevantes.

Perguntas úteis:

1. Dois estados com a mesma média produzem decisões diferentes?
2. Precisamos saber quem, de quem, onde ou desde quando?
3. Algum compromisso/consequência precisa sobreviver ao tick?
4. Outro ator pode desconhecer ou interpretar incorretamente o estado?

Se sim, representação explícita pode ser justificada.

Isso não obriga simulação microscópica.

---

# Parte IV — Tempo, determinismo e configuração

## 11. Tick base = um dia

**DECIDIDO**

O tick principal continua sendo um dia.

Nem tudo precisa rodar diariamente.

Exemplos:

| Frequência | Uso provável |
|---|---|
| diária | ações, viagem, efeitos ativos |
| semanal/mensal | avaliações agregadas, migração |
| mensal | impostos/salários/demografia conforme política |
| vencimento | contratos, eleições, mandatos |
| evento | morte, vacância, transferência |
| derivada | idade, tempo desde observação |

Evitar framework temporal gigante; contador absoluto + calendário + `nextEvaluationDay`/vencimentos resolvem muitos casos.

Uma ocorrência de calendário ou regra de recorrência determina quando uma
operação é considerada. A mudança factual, se houver, pertence à autoridade
validada do domínio ou do GM, não ao calendário por si só.

`CALENDAR OCCURRENCE != DOMAIN EFFECT`

### AdvanceDay

`SimulationRuntime.AdvanceDay` não é depósito universal de features. Sistemas explícitos/event-driven não devem ser adicionados ao loop diário sem semântica temporal real.

### Semântica temporal de Active/Dormant

`Dormant` não significa congelamento. Estado derivado do calendário continua
derivado do calendário; vencimentos continuam vencendo; eventos continuam
podendo afetar o indivíduo; processos temporais próprios, como viagem ou
compromissos, seguem sua semântica mesmo sem `EvaluateAction` de alta
frequência.

Cada domínio deve definir como seu estado temporal progride sem depender da
execução diária completa da Utility AI. Active/Dormant não determina, por si
só, a cadência de todos os sistemas.

---

## 12. Determinismo

**DECIDIDO**

A garantia fundamental é **AUTHORITATIVE DETERMINISM**.

Dada:

- a mesma versão compatível da simulação;
- o mesmo estado autoritativo inicial;
- a mesma configuração efetiva;
- o mesmo calendário efetivo;
- o mesmo conteúdo/definições efetivos e compatíveis;
- o mesmo estado determinístico de aleatoriedade;
- e a mesma sequência logicamente ordenada de comandos externos;

todos os hosts suportados devem produzir os mesmos resultados autoritativos
nas mesmas fronteiras lógicas.

Unity, headless e o futuro `Simulation.Core` não representam simulações
conceitualmente diferentes. A garantia é de **equivalência semântica
autoritativa**, não necessariamente de igualdade bit-a-bit de logs, diagnostics,
objetos transitórios ou detalhes puramente técnicos. Se um identificador ou
detalhe técnico participar da causalidade ou da persistência, ele deixa de ser
puramente técnico e passa a fazer parte do estado relevante para o contrato.

### Calendário efetivo e independência do host

`SimulationCalendar` pode permanecer separado de
`EffectiveSimulationConfiguration`. Ainda assim, o calendário efetivo faz
parte das entradas autoritativas da execução. Uma execução determinística
equivalente exige, além dos demais elementos desta garantia:

```text
same effective configuration
same effective calendar
same compatible content/version
same authoritative state
same deterministic randomness and input sequence
```

Dois hosts com a mesma configuração raiz, mas calendários semanticamente
diferentes, não estão executando o mesmo contexto determinístico. Unity,
headless e um futuro `Simulation.Core` podem possuir composition roots
diferentes, mas não podem interpretar a mesma configuração efetiva com
policies autoritativas diferentes.

```text
Host-specific composition may choose HOW to execute.
It must not redefine WHAT the effective simulation policy is.
```

Não se deve criar um `EffectiveSimulationContext` apenas para agrupar esses
elementos sem uma necessidade semântica concreta.

Hosts podem instanciar infraestrutura diferente quando isso não altera
`World Truth`. Um host pode manter queries ou adapters de um domínio
desabilitado e outro nem instanciá-los, desde que ambos produzam o mesmo
resultado autoritativo. A presença de `MonoBehaviour`, provider, store ou
serviço não altera por si só a policy do mundo.

Se a configuração efetiva exigir um domínio que não está disponível no host,
a execução não é uma simulação equivalente parcialmente degradada: a
composição deve falhar explicitamente.

### Aleatoriedade causal

A aleatoriedade pertence à simulação, não ao estado global incidental do host.
O consumo de aleatoriedade por uma operação semanticamente não relacionada não
deve, por si só, alterar o resultado de outra operação.

Isso não significa que os sistemas sejam causalmente isolados. Se um sistema
altera um estado do mundo que outro sistema consome, o segundo pode
legitimamente produzir resultado diferente:

```text
semantic state change → downstream result may change
incidental unrelated RNG consumption → downstream result should not change
```

Streams, RNG stateful, derivação determinística por contexto ou combinações
dessas estratégias continuam sendo decisões de implementação futuras. A
arquitetura fixa a propriedade causal, não uma implementação universal de RNG.

Uma seed pode iniciar uma execução, mas não representa sozinha todo o estado
determinístico necessário para repetir ou continuar uma execução. Estado,
posição ou contexto de aleatoriedade devem ser preservados quando influenciarem
resultados futuros.

### Ordem semântica

Qualquer ordem de processamento capaz de alterar `World Truth` deve ser
semanticamente definida ou determinística. A simulação não deve depender de
ordem incidental de `Dictionary`, `HashSet`, enumeração de assets Unity,
descoberta de objetos ou outra ordem acidental do host.

Ordem de prioridade, cronologia, agenda, iniciativa, ID estável ou outra ordem
de domínio pode legitimamente participar da causalidade quando explicitamente
definida. O objetivo não é eliminar toda ordem, mas impedir que uma ordem não
semântica escolha o resultado do mundo.

### Escala, materialização e atividade

`Active/Dormant` e carregado/descarregado são mecanismos de escala, não formas
alternativas da simulação. A disponibilidade de CPU, pressão de memória, ordem
de descoberta Unity, tela aberta ou outra diferença incidental do host não pode
decidir autoritativamente quem recebe uma simulação semanticamente diferente.

Se a classificação `Active/Dormant` for futuramente derivada, ela deverá ser
determinística a partir de estado, configuração e inputs autoritativos. A
política concreta de ativação, cadência, catch-up e persistência do modo de
processamento permanece uma decisão futura.

Materialização é independente de atividade. Materializar uma Person ou
carregar uma representação Unity não deve automaticamente torná-la `Active`,
produzir observações, executar AI, iniciar viagens, consumir RNG ou criar
consequências autoritativas.

### Gênese do mundo e início da história simulada

**DECIDIDO**

O mundo inicial configurado deve estar completamente estabelecido como
`World Truth` antes da primeira fronteira temporal realmente simulada.
Geração inicial pode ser composta de etapas e consumir conteúdo resolvido,
mas produz estado semântico do mundo, não instruções de renderização. Observar
ou carregar uma região inicial não cria retroativamente sua existência.

Um mundo pré-envelhecido pode ter backstory gerada anterior ao início efetivo
da simulação. Ela pode explicar seu estado inicial, mas não é histórico
realmente simulado e não precisa ser forkável internamente. A primeira
fronteira temporal realmente simulada delimita o início da garantia de fork;
o número do dia no calendário, por si só, não define essa fronteira.

Desde o início da história realmente simulada, a geração inicial não possui
mais autoridade especial sobre o mundo. Criação, alteração e destruição passam
pelas authorities normais de runtime e integram `World Truth`. Isso inclui
expansão explícita do mundo e mudanças em cidades, POIs, edifícios, estradas,
recursos, organizações, empregos e propriedades. A origem gerada, manual,
modded ou construída não torna o estado imutável nem cria uma segunda
autoridade.
Qualquer mutação autoritativa durante a história realmente simulada participa
da reconstrução histórica conforme sua fronteira causal.
Uma região criada por expansão só passa a existir nessa fronteira; sua
história simulada não é fabricada retroativamente.
A proveniência do estado inicial e das definições usadas deve permanecer
recuperável quando for necessária para interpretar sua causalidade ou
reconstruí-lo. Ela não concede autoridade de mutação após o início da simulação.

```text
GENERATED BACKSTORY != SIMULATED HISTORY
INITIAL GENERATED STATE != IMMUTABLE WORLD
INITIAL GENERATION != RUNTIME MUTATION AUTHORITY
RUNTIME-CREATED WORLD STRUCTURE = NORMAL WORLD TRUTH
```

---

## 13. Configuração: Policy, Parameters, Content

**DECIDIDO**

Três responsabilidades:

```text
POLICY
Pode acontecer?
Pode acontecer autonomamente?

PARAMETERS
Com que taxa, frequência, velocidade, threshold ou intensidade?

CONTENT
Como este objeto específico funciona?
```

Resolução:

```text
Defaults
→ Preset
→ World overrides
→ Content overrides
→ EffectiveSimulationConfiguration
→ Domain systems
```

### Configuração efetiva é composição, não snapshot de conteúdo

`EffectiveSimulationConfiguration` representa a composição das policies e dos
parameters efetivos resolvidos da simulação. Ela pode ser composta por
configurações efetivas de domínio, por exemplo:

```text
EffectiveSimulationConfiguration
├── EffectivePopulationConfiguration
├── EffectiveTravelConfiguration
├── EffectiveEconomyConfiguration
├── EffectiveMerchantTradeConfiguration
├── EffectiveCommercialKnowledgeConfiguration
├── EffectiveCrimeConfiguration
└── ...
```

Ela não é um snapshot gigante de todo o conteúdo do mundo. Cidades, NPCs,
itens, profissões, statuses, definições de ações e conteúdo local podem
permanecer fora dela quando representam dados ou definições específicos de
objetos, e não uma policy ou parameter efetivo da simulação.

O fato de uma definição participar da resolução não exige que ela seja copiada
para a configuração global. O limite é semântico: policies e parameters que
governam o comportamento autoritativo devem ter um valor efetivo resolvido;
conteúdo específico de um objeto pode continuar sendo consumido pelo domínio
que o possui.

### Uma única autoridade efetiva

Todo parameter ou policy resolvido que governa comportamento autoritativo deve
possuir uma única autoridade efetiva consumida pelo domínio. Assets, presets e
conteúdo podem ser fontes de authoring ou de override, mas deixam de ser
autoridades concorrentes depois da resolução.

```text
authoring source != resolved effective authority
```

Assim, quando um override de conteúdo altera uma policy ou parameter, o fluxo
conceitual é:

```text
content/authoring
→ resolution
→ effective value
→ domain consumer
```

O host pode escolher implementações, adapters e infraestrutura para executar a
simulação, mas não pode redefinir uma policy que já foi resolvida na
configuração efetiva.

### Imutabilidade por execução

Uma execução normal de `SimulationRuntime` possui uma configuração efetiva e
um calendário efetivo imutáveis. A execução não deve reler
`ScriptableObjects`, assets ou outras fontes de authoring silenciosamente para
recalcular policies durante `AdvanceDay` ou durante outras operações.

Se futuramente uma policy puder mudar durante a vida do mundo, a mudança deve
ser uma transição ou comando explícito em uma fronteira lógica. Ela deverá
produzir uma nova configuração efetiva, ou uma revisão identificável da
configuração, e participar do determinismo, do save/load, da ordenação de
comandos, da compatibilidade entre hosts e de diagnostics/history quando isso
for semanticamente relevante. O mecanismo concreto para mudanças de
configuração em runtime permanece uma decisão futura.

### Availability, enablement, autonomy e instanciação

**DECIDIDO**

Esses quatro conceitos pertencem a camadas diferentes e não devem ser
colapsados:

```text
AVAILABLE
    o host possui uma implementação/capability semanticamente compatível.

ENABLED
    o domínio participa da política autoritativa normal daquele mundo/configuração.

AUTONOMOUS
    o domínio pode originar novas intenções ou transições discricionárias sem
    input externo explícito.

INSTANTIATED
    uma implementação concreta está atualmente criada, carregada ou registrada.
```

`INSTANTIATED` não implica `ENABLED`. Um host pode carregar stores, queries,
adapters ou serviços de infraestrutura para um domínio desabilitado, desde que
isso não produza processamento autoritativo espontâneo. Da mesma forma,
`AVAILABLE` não implica que o mundo deseje usar o domínio.

Autonomia pressupõe enablement. Na fronteira de execução, uma policy que exige
um domínio habilitado pressupõe uma capability disponível e compatível, salvo
um mecanismo explícito de lazy loading quando a capability existe, mas ainda
não foi instanciada.

Se a configuração efetiva exigir uma capability que o host não possui, a
composição deve falhar explicitamente. Não é permitido desligar silenciosamente
a policy do mundo para acomodar uma limitação do host.

### Autonomy é origem de novo comportamento

**DECIDIDO**

Autonomy significa originar novo comportamento discricionário. Não são
automaticamente autonomia:

- avançar timers ou cumprir vencimentos;
- processar viagens já iniciadas;
- executar planos, compromissos ou decisões já assumidos;
- aplicar consequências obrigatórias;
- executar comandos externos ou ações solicitadas explicitamente.

Uma reação a evento só é autonomia quando envolve escolher uma nova intenção
discricionária. `AutonomousEnabled` não deve ser introduzido universalmente em
todos os domínios; só deve existir quando houver essa distinção semântica
concreta.

### SimulationModuleSet e providers

O `SimulationModuleSet` atual é um mecanismo local/legado do bootstrap Unity.
Pode permanecer temporariamente por compatibilidade, mas não possui autoridade
semântica canônica sobre enablement ou autonomy e não é requisito conceitual do
futuro `Simulation.Core`.

A direção conceitual é:

```text
EffectiveSimulationConfiguration → policy autoritativa
host capabilities                → AVAILABLE
composition validation           → verifica compatibilidade
composition root                 → INSTANTIATED / implementações concretas
```

Registrar um provider ou serviço não habilita semanticamente um domínio. A
mesma capability de execução pode ser utilizada por uma decisão autônoma, uma
scheduled directive, um `GM Request`, um `GM Declare` ou um `GM ForceOutcome`,
conforme as regras da operação. Provider registration não substitui a policy
de enablement ou autonomy.

### Economy != Merchant != Merchant autonomy

**DECIDIDO**

`Economy` é um domínio independente. `Merchant` é um comportamento ou agente
especializado que consome capacidades econômicas; não é a definição da
existência da economia.

```text
Economy != Merchant != Merchant autonomy
```

Economy pode estar habilitada e operar sem merchants autônomos. A presença de
`MerchantSystem` ou de um provider não habilita Economy nem merchant autonomy
por si só.

### Effective merchant trade configuration

**DECIDIDO**

Existe semântica suficiente para uma configuração efetiva específica das
operações de trade merchant. Ela pode conter policies globais de operação,
como:

- autorização para originar uma nova intenção autônoma de reposicionamento
  comercial;
- limite máximo por operação ou plano comercial;
- outras policies globais de execução comercial quando justificadas.

O campo atual `allowMerchantTradeRepositioning` deve ser interpretado como:

```text
permission to originate a NEW autonomous commercial repositioning intention
```

Uma nova intenção discricionária de reposicionamento é autonomia. A execução
ou reavaliação de um compromisso comercial já assumido não é automaticamente
autonomia e não deve ser bloqueada apenas por essa policy.

Uma nomenclatura futura como `AllowAutonomousTradeRepositioning` pode tornar a
semântica mais explícita, mas a arquitetura não exige rename apenas por
documentação.

`maxMerchantTradeAmount` é o limite efetivo máximo permitido por uma operação
ou plano comercial. Ele não representa a capacidade física, financeira ou
pessoal de cada merchant:

```text
actual trade amount
    <= effective operation limit
    <= outras restrições aplicáveis
```

Capacidade de carga, capital, estabelecimento ou job pode impor limites
adicionais independentemente. O limite global pertence à configuração efetiva
de merchant trade.

### Minimum profit é conteúdo de comportamento mercantil

**DECIDIDO**

`minimumProfitPerItem` não é uma regra global de validade econômica. Ele
representa a estratégia ou preferência de um merchant, ou de sua definição de
job: quanto lucro aquele agente exige para considerar uma oportunidade
atraente.

Merchants diferentes podem futuramente possuir thresholds diferentes. O valor
não deve ser movido para uma `UniversalAgentConfiguration` nem copiado para a
configuração efetiva global apenas porque hoje o bootstrap fornece um valor
único.

`PreferredTradeItems`, `utilityMultiplier`, personalidade, capacidades
individuais e conteúdo específico de merchant ou job permanecem content e não
pertencem à configuração efetiva global.

### Effective commercial knowledge configuration

**DECIDIDO**

Os parâmetros de conhecimento comercial formam uma configuração efetiva
coerente, consumida por todos os sistemas relevantes do domínio:

```text
EffectiveCommercialKnowledgeConfiguration
├── freshForDays
├── maxUsefulAgeDays
└── maxSharedObservationsPerInteraction
```

Eles alteram o que os atores sabem, por quanto tempo uma observação permanece
útil e quanto conhecimento pode ser compartilhado em uma interação. Portanto,
alteram decisões autoritativas e não podem permanecer como detalhes do
bootstrap Unity.

### Constantes comerciais autoritativas

**DECIDIDO / DIREÇÃO**

Constantes comerciais que alteram resultados autoritativos não devem
permanecer como autoridade escondida de implementação. Isso inclui, por
exemplo:

- `LocalMerchantWholesalePriceMultiplier`;
- `LocalMerchantReserveRatio`;
- `MaxUnprofitablePlanWaitDays`.

Uma implementação futura deve torná-las entradas autoritativas explícitas na
configuração de domínio apropriada, preservando inicialmente sua semântica
global atual. Essa decisão não transforma essas regras em características
individuais apenas para permitir variação futura.

### Authoring comercial e autoridade efetiva

`SimulationConfigData`, `ScriptableObjects` e outros assets podem continuar
sendo fontes de authoring. Depois da resolução, o fluxo deve ser:

```text
authoring
→ effective merchant/commercial configuration
→ systems
```

O domínio não deve consultar uma segunda autoridade bruta concorrente. Nenhuma
policy comercial autoritativa deve existir apenas como campo serializado de
`TesteSimulacao` ou como constante privada de `MerchantSystem`. Unity,
headless e `Simulation.Core` devem receber semântica comercial equivalente a
partir do mesmo contexto efetivo.

Preset é dados, não código especial.

O usuário casual pode usar presets. O simulacionista pode editar configurações avançadas.

---

# Parte V — Ações, decisões, objetivos e planos

## 14. Action != Goal != Plan != Preference

**DECIDIDO**

| Conceito | Pergunta | Exemplo |
|---|---|---|
| Action | o que tento agora? | comprar ferro |
| Goal | qual resultado quero alcançar? | obter 500 moedas para comprar uma loja |
| Plan | qual compromisso/etapas estou mantendo? | comprar carga → viajar → vender |
| Preference | o que costumo valorizar? | riqueza, segurança |

Não criar `GoalSystem` universal antecipadamente.

Planos especializados podem usar máquinas de estado pequenas e explícitas.

Compromissos podem ser suspensos/cancelados por motivos registrados. Ex.: fuga por segurança suspende plano comercial.

---

## 15. Utility AI continua adequada

**DECIDIDO**

Utility AI continua respondendo:

```text
Quais ações são possíveis?
Quanto interessam agora?
Qual será tentada?
```

Novos domínios alimentam avaliação:

- goal/intenção;
- memória;
- relação;
- personalidade;
- organização;
- conhecimento;
- habilidade/capacidade.

**Habilidade não deve automaticamente aumentar interesse.** Ela pode alterar chance, qualidade, custo e benefício esperado.

Behavior Trees/GOAP só entram se problemas concretos justificarem.

### Candidatos múltiplos

Se uma ação gerar vários alvos, cuidado para não multiplicar artificialmente o peso da família de ação. Pode ser necessário escolher família e depois alvo, ou normalizar orçamento de peso.

---

# Parte VI — Conhecimento, memória, interpretação e informação

## 16. Knowledge

**DECIDIDO**

Knowledge responde:

> “o que este ator acredita/sabe sobre algo?”

Um registro útil pode conter:

- assunto tipado;
- valor/estimativa;
- fonte;
- dia observado;
- dia recebido;
- confiança;
- precisão;
- atualidade;
- proveniência.

### Princípios

- informação confiável pode estar velha;
- informação antiga não se atualiza sozinha;
- desconhecido permanece desconhecido;
- informações conflitantes exigem regra explícita simples;
- várias cópias derivadas da mesma origem não são confirmações independentes;
- propagação consome contato, tempo e alcance; não é broadcast global.

---

## 17. Knowledge holders não são apenas NPCs

**DECIDIDO / DIREÇÃO**

O princípio `WORLD TRUTH != KNOWLEDGE` se aplica potencialmente a:

- Person/NPC;
- Faction;
- Institution.

Conhecimento institucional/faccional não implica que todos os membros individualmente saibam a mesma coisa.

Não criar imediatamente uma superclasse universal `KnowledgeHolder`; preservar a possibilidade através de contratos adequados.

---

## 18. Memory != Knowledge

**DECIDIDO**

Memory responde:

> “que experiência relevante este ator reteve?”

Knowledge responde:

> “o que ele acredita atualmente sobre o estado de algo?”

Exemplo:

```text
Memória:
“A Ordem apoiava Arthur quando meu pai morreu.”

Conhecimento atual:
“A Ordem agora apoia Beatriz.”
```

A atualização do conhecimento não deve apagar automaticamente a memória histórica.

---

## 19. Esquecimento é gradual e dependente de saliência

**DECIDIDO / DIREÇÃO**

Memória não deve ser simples TTL uniforme.

Uma memória pode possuir fatores como:

- importância;
- relevância pessoal/emocional;
- repetição/reforço;
- idade;
- confiança;
- classe de persistência.

Exemplos:

```text
“vi um mercador comprar pão”
→ baixa saliência → pode desaparecer

“ele me emprestou algumas moedas”
→ média saliência → pode enfraquecer

“ele salvou minha vida”
→ alta saliência → memória muito duradoura

“ele matou meu filho”
→ extrema saliência → não deve sumir por decay comum
```

Traços individuais, como **esquecido/distraído**, podem modificar taxa de deterioração. Não devem fazer todo tipo de memória desaparecer igualmente.

Eventos centrais de vida, grandes traições, salvamentos, mortes próximas e juramentos importantes podem ter persistência excepcional.

---

## 20. Knowledge != Interpretation

**DECIDIDO**

Um ator pode conhecer corretamente uma regra/fato e deliberadamente aplicar interpretação heterodoxa.

Exemplo:

```text
Known rule:
“filho homem mais velho tem precedência.”

Known fact:
Arthur é o filho homem mais velho.

Interpretation:
“excomunhão remove a precedência.”

Recognition:
Beatriz tem claim reconhecido.

Support:
Beatriz.
```

A posição heterodoxa não deve ser modelada automaticamente como ignorância.

### Justification

**DIREÇÃO**

Decisões políticas importantes devem poder carregar razões/referências estruturadas suficientes para explicar o resultado sem depender apenas de `score = 84`.

---

# Parte VII — Relações e reputação

## 21. Appraisal != Reaction != Relationship

**DECIDIDO / DIREÇÃO**

Esses conceitos pertencem a camadas diferentes e não devem ser colapsados:

```text
APPRAISAL
    → avaliação derivada de um fato ou consequência,
      usando conhecimento, interpretação e estado relevante do avaliador

REACTION
    → resposta significativa e específica de um evento,
      produzida por uma appraisal

RELATIONSHIP
    → estado persistente entre atores,
      potencialmente influenciado por múltiplas reactions
```

Preservar:

```text
APPRAISAL != REACTION
REACTION != RELATIONSHIP
REACTION != SUPPORT
SUPPORT != POLITICAL POSITION
```

Uma appraisal pode ser uma operação pura ou derivada. Nem toda appraisal
precisa ser persistida. Quando uma appraisal produzir uma reaction
significativa que precise influenciar decisões futuras, essa reaction se torna
estado autoritativo persistente do mundo simulado. Isso é verdade sobre o
estado cognitivo ou social registrado para o avaliador, não uma afirmação de
que sua interpretação esteja factualmente correta.

Uma relationship futura será outro estado autoritativo, com store, lifecycle
e semântica próprios. Ela poderá resumir ou projetar múltiplas reactions, mas
não substituirá automaticamente a história das reactions que a influenciaram.

Reaction não é `DomainEvent`, e `DomainEvent` não é sua autoridade primária.
Um evento pode ser registrado para history, diagnostics ou UI, enquanto a
reaction significativa pertence ao estado semântico que possui sua regra.

### Fonte semântica e identidade da reaction

Uma reaction deve referenciar um fato ou outcome semântico estável do domínio.
Não é necessário criar agora um `WorldFactId` universal: cada domínio pode
fornecer a identidade persistente adequada para os outcomes que podem ser
avaliados socialmente.

A source identity não deve depender de:

- `NpcRuntimeId`;
- Unity instance ID;
- descoberta de objetos ou ordem de carregamento;
- `DomainEvent.EventId` usado automaticamente como identidade de truth ou
  outcome.

Isso preserva a distinção entre o fato/outcome autoritativo, sua representação
histórica e as projeções runtime do host.

### Revisão sem retcon

Conhecimento novo pode criar uma nova appraisal ou reaction, mas não deve
reescrever a cognição histórica:

```text
Dia 10:
Maria sabe apenas que foi roubada.
→ R1: target = TheftOutcome; attribution = Unknown

Dia 15:
Maria acredita que João foi o autor.
→ R2 supersedes R1

Dia 20:
Maria acredita posteriormente que Pedro foi o autor.
→ R3 supersedes R2
```

R1 continua sendo historicamente verdadeiro como estado cognitivo do dia em
que foi criado. A lineage mínima inicial é uma `ReactionId` com referência
opcional a `SupersedesReactionId`. Não introduzir um `ReactionLineageId`
adicional sem consumidor concreto.

Os registros devem preservar a sequência autoritativa. A visão corrente deve
ser derivável ou reconstruível a partir dela: dentro de cada thread de
avaliação, a reaction que não foi superseded é a visão corrente. Um store pode
manter índices ou caches current por performance, mas esses índices não são
uma segunda autoridade nem devem criar uma truth duplicada baseada em flags
mutáveis concorrentes.

O mínimo temporal comum é:

- reaction current;
- reaction anterior superseded;
- histórico preservado.

`HistoricalOnly` pode ser uma classificação de consulta, não necessariamente
um estado persistido. `Resolved` não é estado universal: um caso pode ser
resolvido enquanto a reaction continua relevante. Reparação, satisfação,
perdão ou encerramento são semânticas próprias dos domínios que precisarem
delas.

### Target != perceived attribution

Uma reaction deve separar:

```text
Target:
    “a que estou reagindo?”

Perceived attribution:
    “quem ou o que acredito ter causado ou ser responsável?”
```

Por exemplo:

```text
target = TheftOutcome
attribution = Unknown
```

Posteriormente:

```text
target = TheftOutcome
attribution = BelievedPerson(João)
```

Também pode existir uma reaction separada:

```text
target = João
basis = believed responsibility for TheftOutcome
```

Attribution é estado cognitivo do avaliador, não responsabilidade factual.
No primeiro slice, sua semântica conceitual pode distinguir:

- `NotApplicable` — não há atribuição causal aplicável;
- `Unknown` — existe causa ou responsável relevante, mas não identificado;
- `BelievedPerson(PersonId)` — o avaliador atribui responsabilidade a uma
  Person conhecida.
- `BelievedInstitution(InstitutionId)` — o avaliador atribui responsabilidade
  a uma Institution conhecida.

Não adicionar `Faction`, `Polity` ou `Organization` attribution apenas por
extensibilidade. Esses tipos podem entrar quando houver consumidor concreto.
`Unknown` nunca é um `PersonId` especial.

### Knowledge-gated appraisal

Appraisal de uma Person não pode usar `World Truth` escondida como se fosse
conhecimento do avaliador. Ela precisa de uma base cognitiva disponível para
essa Person, que pode vir de:

- consequência diretamente experimentada;
- observação direta;
- fato conhecido ou recebido;
- believed attribution;
- outcome institucional conhecido.

Exemplo:

```text
World Truth:
João roubou Maria.

Maria experienced:
perdeu dinheiro.

Maria knows:
foi roubada.

Maria does not know:
João foi o autor.
```

É válido produzir uma reaction negativa ao `TheftOutcome` com attribution
`Unknown`. Não é válido produzir uma reaction direcionada ou atribuída a João
até que Maria possua base cognitiva para isso.

```text
NO KNOWLEDGE OF RESPONSIBLE ACTOR
!=
NO REACTION TO SUFFERED CONSEQUENCE
```

### Significance, valence e salience

Cada domínio decide se uma appraisal produz `NoReaction` ou uma reaction
persistida. Não existe threshold numérico universal.

Para a foundation inicial:

```text
Valence:
    Positive | Negative

Salience:
    Low | Medium | High | Exceptional
```

Neutral normalmente significa ausência de reaction persistida. `Explicit
Neutral` só deve existir quando um domínio precisar distinguir “avaliado como
neutro” de “não avaliado”. As categorias de salience são contextuais e não
formam uma escala universal entre crime, política, comércio ou outros
domínios. Não introduzir uma magnitude geral como `-57` ou `+83`.

### Relationships não são projeção automática

C1 e C2 não devem executar automaticamente transformações como:

```text
Relationship -= X
Trust -= Y
Support += Z
```

Uma reaction é uma entrada disponível para consumidores futuros. `PoliticalSupport`
continua com store, lifecycle e semântica próprios. Não promover o
`NpcRelationRuntime` atual a foundation canônica: sua identidade baseada em
`NpcRuntimeId` é adequada apenas à feature local/runtime existente, não a uma
relationship persistente futura.

A direção futura aceita para uma relationship pessoal é que relações sejam
esparsas e direcionadas: a confiança de A em B não precisa igualar a de B em
A. Afinidade, confiança e medo são dimensões possíveis, não um contrato
universal já fechado. Uma futura relationship pode combinar resumo persistente
para decisões rápidas com episódios significativos para explicação, mas essa
projeção não faz parte de C1/C2.

Parentesco não é relação afetiva.

### Escopo conceitual do Checkpoint C

O Checkpoint C fica dividido em:

```text
C1 — Social Appraisal Foundation
C2 — Crime/Justice vertical slice
C3 — Persistent Relationship Projection (DEFERRED)
```

C1 define a semântica de appraisal, reaction, attribution, significance,
proveniência, revisão sem retcon e determinismo. C2 valida esses contratos
com Crime/Justice, incluindo vítima que conhece a perda mas não o perpetrator,
atribuição posterior possivelmente incorreta, investigação conhecida e
reactions de Persons dirigidas à vítima, ao investigador, à Institution ou ao
criminoso conforme o conhecimento efetivamente disponível.

O primeiro evaluator é `Person`, identificado por `PersonId`. A foundation
não depende de materialização de `NpcRuntime` e permanece compatível com
`Person-only`, rich state loaded, `Dormant` e futuro rich state unloaded. Isso
não inventa estado psicológico que uma Person-only não possui: appraisal só
usa informações e estado efetivamente disponíveis.

Appraisal de Faction ou Institution permanece extensão futura. Preservar:

```text
member appraisal != faction appraisal != official faction position
```

Population aggregates ficam fora de C1/C2. A foundation não exige
materializar toda a população, mas deve permanecer compatível com aggregate
impact, aggregate appraisal e population sentiment futuros. Em particular:

```text
aggregate appraisal != sum of Person appraisals
```

C1 e C2 não incluem universal relationship engine, emotion engine, ideology
engine, reputation global, public opinion system, memory system completo,
`UniversalAgent`, `PoliticalActor` universal, social score universal,
appraisal coletiva ou relationship projection automática.

### Determinismo e persistência futura

Appraisal e reaction no primeiro slice devem ser determinísticas por
construção. Criação e revisão não podem depender de materialização de
`NpcRuntime`, descoberta Unity, iteração incidental de collections, UI, CPU ou
ordem de carregamento do host. Não usar RNG sem necessidade concreta; se
randomness surgir no futuro, deverá seguir a aleatoriedade context-scoped já
decidida.

Uma reaction significativa deverá preservar ou reconstruir inequivocamente,
em save/load futuro:

- `ReactionId`;
- evaluator `PersonId`;
- referência ao fato/outcome de origem;
- target;
- attribution e eventual believed source;
- valence;
- salience;
- cognitive/provenance basis;
- creation day;
- supersession lineage.

O C3 só deve ser reaberto quando surgir consumidor concreto que precise
responder algo como:

```text
qual é o estado social persistente entre A e B depois de múltiplos episódios?
```

As dimensões concretas de uma relationship — como trust, affinity, fear,
respect ou gratitude — permanecem abertas até esse consumidor existir.

---

## 22. Reputação é percepção coletiva

**DIREÇÃO aceita**

Reputação pode existir por cidade, organização ou domínio específico.

Não é:

- média obrigatória de todas as relações individuais;
- conhecimento onisciente;
- prova jurídica;
- atualizada por fatos que ninguém conhece.

População agregada pode sustentar reputação coletiva sem milhares de opiniões individuais.

---

# Parte VIII — Organizações, facções e identidade coletiva

## 23. Organization, Faction, Institution e futuro Polity não são automaticamente a mesma coisa

**DECIDIDO / DIREÇÃO / ABERTO**

- **Organization** — identidade coletiva genérica quando houver necessidade
  concreta de representá-la como tal;
- **Faction** — organização/agrupamento atuando em disputa de
  interesses/política;
- **Institution** — estrutura institucional com autoridade, cargos e regras
  próprias;
- **Polity/Realm/State** — conceito futuro provável para reino/Estado que
  agrega território, população e instituições, sem ser automaticamente uma
  Organization.

Esses conceitos podem se sobrepor na realidade ficcional, mas não são
automaticamente subclasses da mesma entidade, o mesmo store, a mesma relação
de membership, a mesma semântica de autoridade ou a mesma política de
continuidade. Uma entidade social pode desempenhar múltiplos papéis sem que
esses papéis sejam colapsados em um único modelo.

`OrganizationRuntime` é uma implementação runtime local/legada. Pode
continuar existindo para a feature atual, mas não possui autoridade para
definir a arquitetura futura de `Organization`, `Faction`, `Institution` ou
`Polity`. Sua existência no código não o torna uma foundation canônica.

O modelo atual de `OrganizationRuntime` usa identidade runtime e associa
membros por `NpcRuntimeId`. Isso é compatível com uma representação local de
execução, mas não define membership histórica persistente. Relações
persistentes de participação individual devem usar identidade persistente
apropriada, normalmente `PersonId`.

Uma futura `Organization` genérica canônica permanece aberta. Ela só deve ser
formalizada quando existir um consumidor concreto que não seja melhor atendido
por `Faction`, `Institution`, `Polity` ou outro domínio específico. Até lá,
`OrganizationRuntime` não deve crescer acidentalmente para ocupar esse papel.

Exemplo futuro:

```text
Kingdom of Valeria       → Polity
Royal Council            → Institution
House of the Lion        → Faction
Merchant Guild           → Organization/Institution com atuação política
```

**Não criar `Polity` antes de haver consumidor real**, mas não deixar outro tipo assumir silenciosamente seu significado.

---

## 24. Profissão != cargo

**DECIDIDO**

`Merchant` é comportamento/profissão.

Esse comportamento não define a existência da economia nem implica autonomia
mercantil. `Economy`, `Merchant` e `Merchant autonomy` permanecem conceitos
distintos.

`Prefeito`, `Rei`, `General do Conselho` são cargos/autoridade institucional.

Uma pessoa pode mudar profissão e continuar em um cargo, ou perder um cargo e manter sua profissão.

---

## 25. Office != Title != Social Status

**DIREÇÃO**

Exemplos:

- Rei de X — pode ser cargo.
- Duque — pode ser título, cargo, ou ambos conforme setting.
- Nobre — status social, não necessariamente cargo.

Não colapsar automaticamente esses conceitos.

---

## 26. Faction affiliation != support != loyalty != obedience

**DECIDIDO**

Ser membro de uma facção não implica:

- apoiar a posição oficial;
- concordar com a liderança;
- conhecer tudo que a facção sabe;
- obedecer toda ordem;
- permanecer membro para sempre.

Exemplo:

```text
Helena ∈ House of the Lion
Official faction position → supports Arthur
Helena personally → supports Beatriz
```

Isso permite dissidência, disciplina, expulsão, cisma e mudança política emergente.

`Membership` não deve ser interpretada universalmente como office holding,
support, allegiance, citizenship ou authority. Uma futura Organization pode
precisar de uma relação de membership própria, mas sua semântica deve ser
definida pelo domínio concreto. `FactionAffiliation` não é, por padrão, um
sistema universal de membership.

---

## 27. Membership policy é específica por facção

**DECIDIDO**

A infraestrutura deve permitir políticas diferentes, por exemplo:

```text
Faction A:
CanLeave = false
CanRejoin = false

Faction B:
CanLeave = true
CanRejoin = false

Faction C:
CanLeave = true
CanRejoin = true

Faction D:
CanExpel = true
```

Não transformar acidentalmente uma limitação de storage em regra de mundo.

Se reafiliação for permitida, preservar períodos históricos distintos.

```text
Roberto / Guild
100–110 membro
125–atual membro
```

Essas políticas são semântica de afiliação de Faction. Não transformar uma
limitação de storage ou uma política de uma facção em regra universal para
todo vínculo organizacional.

---

## 28. Posição oficial de uma facção é uma decisão coletiva/institucional

**DECIDIDO**

```text
Faction official position
!= raw sum of member positions
!= simple majority by default
```

A posição depende da governança da facção.

Possibilidades futuras:

- líder decide;
- conselho decide;
- cargos têm pesos diferentes;
- voto igualitário;
- modelo misto.

Não criar um `PoliticalPower` universal no Person.

A relevância de um membro depende do **contexto e regra de decisão**.

---

## 29. Authority != Influence

**DECIDIDO**

- **Authority** — direito formal/institucional de produzir determinada decisão.
- **Influence** — capacidade de alterar decisões/comportamento de atores.

Exemplo:

```text
Royal Council:
autoridade formal para selecionar sucessor.

General:
sem autoridade formal, mas controla o exército.

Archbishop:
sem voto formal, mas possui enorme influência religiosa.
```

Isso permite que um resultado seja legalmente válido e politicamente frágil.

`Leader` não implica authority formal. Membership não implica office holding,
e uma Organization não é automaticamente uma Institution. A autoridade deve
continuar pertencendo ao domínio que define o direito de produzir a decisão.

---

## 30. Continuidade de organizações através do tempo

**DECIDIDO**

### Renomeação

```text
rename
→ mesmo FactionId / OrganizationId
```

Mudança de liderança, muitos membros ou até ideologia não cria automaticamente nova identidade.

### Cisma

Quando existe ruptura estrutural real:

```text
Faction F
→ encerra como organização unificada
→ Faction A (novo ID)
→ Faction B (novo ID)
```

**Todos os lados do cisma recebem novos IDs.**

Cada sucessora pode reivindicar ser a continuação legítima da antiga, mas:

```text
IDENTITY != CLAIMED CONTINUITY
```

Factualmente, ambas são sucessoras de F.

Politicamente, cada uma pode declarar:

> “Somos a verdadeira organização fundada há 500 anos.”

### Fusão

**DECIDIDO**

- fusão em organização realmente nova → novo ID, predecessoras encerradas;
- absorção de B por A → A mantém identidade, B encerra.

### Secessão

Quando o original claramente continua existindo, o original preserva seu ID e
a parte separatista recebe um novo ID. Quando a ruptura encerra a organização
anterior como unidade e produz sucessoras, aplica-se a regra de cisma: todos os
sucessores recebem novos IDs.

A decisão deve refletir continuidade semântica, não nome textual.

---

# Parte IX — Política, claims, legitimidade e decisão coletiva

## 31. Claim é afirmação política, não verdade

**DECIDIDO**

Um `PoliticalClaim` pode existir sem ser factual, reconhecido ou apoiado.

Exemplos:

- direito a cargo;
- sucessão;
- propriedade;
- autoridade institucional;
- status;
- linhagem;
- futuramente continuidade histórica de organização.

Criar claim não deve:

- ocupar cargo;
- transferir propriedade;
- reescrever genealogia;
- mudar morte factual;
- criar automaticamente legitimidade.

---

## 32. Vários claimants podem disputar o mesmo alvo

**DECIDIDO**

Exemplo:

```text
Rei morreu.
Filho A e filha B são factual/estruturalmente elegíveis.

A → Claim ao trono
B → Claim ao trono
```

Várias instituições, facções e pessoas podem apoiar lados diferentes.

Não existe regra universal `target -> one claimant`.

---

## 33. Reconhecimento é por instituição, não uma verdade global

**DECIDIDO**

O mesmo claim pode ser simultaneamente:

```text
Council → Recognized
Church  → Rejected
Guild   → Contested
```

Logo, a semântica de longo prazo é uma relação:

```text
Institution → Claim → Recognition state/history
```

Uma representação singular dentro do claim só é aceitável se explicitamente limitada a um caso de uso que não exige múltiplos reconhecedores.

Reconhecimento não torna o claim factual.

---

## 34. Recognition != Support

**DECIDIDO**

Uma instituição pode reconhecer A como o claimant convencionalmente correto e politicamente apoiar B.

Exemplo:

```text
Church:
Recognizes Arthur's conventional claim
Supports Beatriz for political reasons
```

Isso pode depender de interpretação heterodoxa, interesses, alianças ou riscos.

---

## 35. Legitimidade é derivada e contextual

**DECIDIDO**

Não criar:

```csharp
person.Legitimacy = 82;
```

como verdade universal.

Legitimidade é avaliação que pode depender de:

- elegibilidade factual;
- genealogia;
- claim basis;
- reconhecimento;
- apoio;
- tenure/história;
- regras/normas;
- interpretação;
- perspectiva do avaliador;
- contexto/cargo.

Conceito desejado:

```text
LegitimacyAssessment
- subject
- context
- perspective
- factors
- result
```

Se houver score, escala e semântica devem ser explícitas e derivadas.

---

## 36. Support é posição sobre alvo específico

**DECIDIDO**

Evitar `PoliticalSupport = 73` sem alvo.

Apoio/oposição deve apontar para algo, como:

- PoliticalClaim;
- candidato em disputa específica;
- futura policy/decision quando houver domínio.

Apoio pode vir de Person, Faction ou Institution conforme semântica.

Afiliação não gera support automaticamente.

---

## 37. Influência política é contextual

**DECIDIDO**

Não usar contagem bruta universal.

Numa sucessão:

- líder de facção pode valer muito;
- comandante militar pode valer muito;
- autoridade religiosa pode valer muito;
- membros comuns podem valer pouco individualmente.

Em outra decisão, pesos/autoridades mudam.

Portanto:

```text
more support generally matters
```

mas não:

```text
if supportA > supportB => A wins
```

A `Decision Policy` do contexto define como posições se tornam resultado.

---

## 38. Seleção política != assunção do cargo

**DECIDIDO**

Pode existir separação temporal:

```text
Day 100: ruler dies
Day 103: council selects Arthur
Day 110: coronation/investiture
Day 110: Arthur formally assumes office
```

Entre seleção e posse:

- selecionado pode morrer;
- perder condição;
- decisão pode ser contestada;
- cerimônia pode falhar;
- golpe/intervenção pode ocorrer.

Execução final deve revalidar a verdade atual.

Cargos simples podem continuar com execução imediata; investidura atrasada é capability do conteúdo/instituição, não obrigação universal.

---

## 39. Political disagreement != Civil War

**DECIDIDO**

Apoio semelhante, claims concorrentes ou reconhecimento dividido produzem **disputa**, não guerra automática.

Cadeia desejada:

```text
competing claims
→ recognition/support disagreement
→ political dispute
→ decisions about accept/resist
→ negotiation / sanction / discipline / exclusion
→ possible schism / mobilization
→ possible physical conflict
```

Não implementar universalmente:

```text
if supportDifference < X:
    StartCivilWar()
```

Escalada precisa responder a algo equivalente a:

> “vale a pena?”

considerando benefício esperado, custo, risco, força, apoio, recursos, aliados e preferências.

---

## 40. Briga interna de facção pode gerar disciplina, expulsão e cisma

**DECIDIDO / DIREÇÃO**

Possíveis resultados de conflito interno:

- tolerância;
- punição;
- disciplina;
- expulsão;
- oposição interna;
- desafio à liderança;
- recusa de decisão oficial;
- cisma;
- nova facção;
- eventualmente violência.

Cisma é resultado político/social. Guerra é possível consequência posterior.

---

# Parte X — Propriedade, custódia, jurisdição e obrigações

## 41. Ownership != Inventory/Custody

**DECIDIDO**

Inventário/custódia responde:

> “onde/com quem o objeto está?”

Ownership responde:

> “a quem pertence?”

Exemplo:

```text
Espada pertence ao Rei.
Guarda está fisicamente com a espada.
```

Não são o mesmo estado.

---

## 42. Ownership != Jurisdiction != Control != Allegiance

**DECIDIDO**

Uma cidade pode ter:

```text
Legal jurisdiction → Kingdom A
Land ownership     → House Martins
Military control   → Rebel Army
Population loyalty → mixed
```

Quatro perguntas diferentes:

- quem possui?
- qual autoridade legal se aplica?
- quem controla de fato?
- quem é apoiado/obedecido pela população?

Essa distinção será crítica para guerra, ocupação, tributação e rebelião.

---

## 43. Titularidade futura não deve ficar eternamente limitada a Person

**DIREÇÃO, não implementação imediata**

A foundation atual pode ser PersonId-based. Futuramente pode ser necessário que ativos pertençam a:

- Person;
- Institution;
- Faction/Organization;
- Polity.

Não generalizar antes de existir consumidor real.

**ABERTO:** modelo de copropriedade/percentuais ainda não está decidido.

---

## 44. Obrigações são compromissos verificáveis, não relação universal

**DIREÇÃO aceita**

Salário, dívida, aluguel e entrega podem compartilhar:

- partes;
- prestação;
- valor;
- vencimento;
- estado;
- motivo de inadimplemento.

Mas casamento, juramento, amizade e lealdade não devem ser reduzidos a `UniversalContract`.

---

## 45. Fluxos monetários devem ter fronteiras explícitas

**DIREÇÃO aceita**

Antes de economia pública sofisticada, toda transação importante deve poder responder:

```text
Quem paga?
Quem recebe?
Qual saldo limita a operação?
Existe source/sink deliberado?
```

Um sink abstrato pode existir se for deliberado. Um pedágio com objetivo fiscal deve ter recebedor.

---

# Parte XI — Demografia, família, residência e migração

## 46. Residence != Presence

**DECIDIDO**

Estar fisicamente numa cidade não altera automaticamente residência.

Visitante não é automaticamente residente.

Migração e viagem são conceitos diferentes.

---

## 47. Genealogy != Relationship != Household != Dynasty

**DECIDIDO / DIREÇÃO**

- genealogia — parentesco estrutural;
- relação — afinidade/confiança/medo etc.;
- household — unidade de coabitação/economia doméstica;
- dinastia — continuidade político-familiar derivada/definida.

Exemplo:

```text
Arthur
biological father → Eduardo
adoptive/social household → Roberto/Helena
dynasty → House Lion
```

Não transformar `GenealogyStore` em family/household system universal.

---

## 48. Parentesco não é afeto

**DECIDIDO**

Um pai odiado continua sendo pai.

Afeto/hostilidade pertence ao sistema de relações, não à genealogia.

---

## 49. Morte preserva identidade

**DECIDIDO**

A morte:

- encerra capacidade de agir;
- não apaga Person;
- não apaga automaticamente propriedade, família, histórico ou claims;
- pode acionar consequências downstream explícitas;
- não equivale automaticamente a vacancy reconhecida.

---

## 50. Migração agregada e individual

**DIREÇÃO aceita**

- população comum → fluxos agregados periódicos;
- Person/NPC relevante → decisão individual + `TravelSystem` + mudança explícita de residência.

Histerese/custo/compromisso mínimo evitam ping-pong por pequenas oscilações.

---

# Parte XII — Saúde e necessidades

## 51. Não simular necessidades microscópicas sem valor decisório

**DIREÇÃO aceita**

Pressões úteis de alto nível:

- subsistência;
- segurança;
- condição econômica;
- saúde.

Moradia entra quando propriedade/aluguel/capacidade urbana importar.

Não criar barras diárias de higiene, refeição e descanso apenas por realismo.

---

## 52. Saúde: detalhe proporcional à relevância

**DIREÇÃO aceita**

NPC relevante:

- condição ativa;
- gravidade;
- duração;
- incapacidade;
- risco.

População agregada:

- grupos afetados;
- taxas;
- capacidade de tratamento.

Não simular anatomia ou cada contato infeccioso sem necessidade.

---

# Parte XIII — Crime, lei, investigação e justiça

## 53. Fato criminal != conhecimento != acusação != condenação

**DECIDIDO / DIREÇÃO**

Cadeia desejada:

```text
real occurrence
→ traces / perceptions
→ obtained information
→ investigative hypothesis
→ judicial measure
→ institutional judgment
→ justice enforcement
```

### Crime, enablement e autonomia

**DECIDIDO**

`EffectiveCrimeConfiguration` preserva duas políticas distintas:

- `Enabled` — o domínio criminal participa do processamento normal daquele
  mundo;
- `AutonomousEnabled` — NPCs podem originar novas ações criminais
  discricionárias autonomamente.

Assim:

```text
Crime.Enabled = true
Crime.AutonomousEnabled = false
```

significa que o domínio criminal está habilitado, mas NPCs não originam novas
ações criminais autonomamente. Operações explícitas suportadas, consequências,
diretivas e comandos externos podem continuar funcionando conforme suas
próprias regras.

`Crime.Enabled = false` desabilita o processamento criminal normal daquele
mundo, mas não torna fatos relacionados ao domínio ontologicamente
impossíveis. Um `GM Declare` pode impor um fato suportado por um caminho
canônico sem ligar automaticamente o domínio.

```text
underlying world fact
    != legal/criminal classification
    != institutional judgment
    != enforcement
```

`Request` continua sujeito às regras normais do domínio e pode ser rejeitado
quando ele estiver desabilitado. `ForceOutcome` continua limitado a outcomes e
caminhos estruturalmente suportados; autoridade externa não cria uma
capability inexistente no host.

A autoria verdadeira não deve ficar disponível ao investigador só porque o sistema conhece a verdade.

---

## 54. Lei != fato

**DECIDIDO**

```text
Roberto killed João
```

é um fato de domínio.

Se isso é:

- assassinato;
- legítima defesa;
- execução legal;
- acidente;
- ato de guerra;

é interpretação jurídica sob uma jurisdição/norma.

Condenação é ainda outro fato institucional.

---

## 55. Evidence e clues

**DIREÇÃO aceita**

Uma pista pode existir e nunca ser descoberta.

Evidência é material/informação obtida e associada a uma hipótese/caso.

Proveniência importa para evitar cinco relatos derivados da mesma fonte contarem como cinco confirmações independentes.

Vestígio físico pode desaparecer enquanto o conhecimento de que foi observado permanece.

---

## 56. “Caso resolvido” é conclusão institucional, não Truth

**DECIDIDO / DIREÇÃO**

`InvestigationCase` é agregado do domínio criminal, não primitive universal.

Uma instituição pode considerar o caso resolvido e estar errada.

Reputação pode orientar busca, mas não vira prova automaticamente.

### Guard/enforcement não é guard autonomy

**DECIDIDO**

`GuardCrime.Enabled` representa a habilitação da integração de enforcement
criminal por guardas. Esse nome não deve ser interpretado automaticamente como
autonomia de guardas.

Sentença, julgamento, enforcement e autonomia de guardas são conceitos
distintos. O comportamento atual que também usa `GuardCrime.Enabled` para
controlar avanço de sentenças é uma decisão de implementação local, não um
contrato arquitetural. Se a autonomia de guardas precisar futuramente de uma
policy própria, ela só deve ser introduzida quando existir uma semântica
concreta que a exija.

### Dependências são específicas da operação

**DECIDIDO**

Dependências de operações concretas não devem virar dependências universais de
um domínio:

```text
implementation dependency != semantic dependency != optional domain integration
```

Crime não exige universalmente `Justice`, `Travel` e `Economy`. Por exemplo,
`Steal` pode exigir transferência econômica ou de propriedade, `Escape` pode
exigir presença e travel, e `Arrest` pode exigir justice/enforcement. Essas
dependências podem ser obrigatórias para a operação específica sem redefinir
silenciosamente a configuração autoritativa do domínio inteiro.

---

# Parte XIV — Capabilities e conflitos locais

## 57. Capability é composição explicável

**DECIDIDO / IMPLEMENTADO COMO FOUNDATION**

Capacidade de um participante pode combinar:

- atributos-base;
- traits;
- itens;
- condições/ferimentos;
- contexto.

A avaliação deve produzir breakdown explicável e imutável.

Configuração deve referenciar definições de atributos, não depender de nomes soltos.

Capability é foundation reutilizável, não sinônimo obrigatório de “força de combate”.

---

## 58. Conflito é resolução abstrata, não combate golpe-a-golpe

**DECIDIDO**

O conflito local suporta:

- N lados;
- NPCs nomeados;
- participantes agregados;
- contexto/modificadores;
- RNG injetável;
- draw;
- upset;
- constraints autorizadas.

O objetivo é resolver **resultado e consequências relevantes**, não simular cada ataque.

---

## 59. Named + aggregate podem coexistir no mesmo conflito

**DECIDIDO**

Uma força pode conter:

```text
General A → NpcRuntime
20 guards → aggregate participant
mercenary captain → NpcRuntime
100 militia → aggregate participant
```

Não criar NpcRuntime para cada soldado.

Participantes agregados são snapshots/representações explícitas da força naquele conflito; IDs de Person/NPC só pertencem a indivíduos reais.

---

## 60. Resolução de conflito

**DECIDIDO / IMPLEMENTADO**

A foundation atual segue a ideia:

```text
participant capabilities
+ structured modifiers
→ side capability
× bounded random factor
→ final scores
→ outcome / side dispositions
```

A aleatoriedade é limitada para permitir surpresa sem apagar completamente a diferença de capacidade. A implementação histórica utilizou fator aproximadamente `[0.8, 1.2]`.

Scores brutos devem permanecer disponíveis quando autoridade externa aplica outcome constraints.

O mesmo NPC não pode participar de lados incompatíveis do mesmo conflito.

---

## 61. Consequência != resolução

**DECIDIDO**

Separar:

```text
Resolve outcome
→ Compute consequences
→ Validate
→ Apply atomically
→ Record event/history
```

Consequência não deve ser aplicada parcialmente antes de saber se o conjunto é válido.

Evento `ConflictResolved` só deve ser registrado após aplicação bem-sucedida.

---

## 62. Estados de vida/ferimento e disposição

**DECIDIDO / FOUNDATION ATUAL**

Ferimentos relevantes usam níveis abstratos, não anatomia detalhada:

```text
None
Hurt
Injured
SeriouslyInjured
Incapacitated
```

Vida factual:

```text
Alive
Dead
```

Disposição de conflito pode representar:

```text
Active
Retreated
Escaped
Surrendered
Captured
Incapacitated
Dead
```

Morte/ferimento devem restringir ações nos pontos de entrada relevantes.

Morte não destrói automaticamente inventário/dinheiro/propriedade; domínios downstream resolvem custódia, estate etc.

---

## 63. Consequências agregadas não inventam um domínio que não existe

**DECIDIDO**

Para aggregate participants, conflito pode produzir:

- perda fracionária;
- capacidade remanescente;
- retirada/surrender etc.

Se não existe store autoritativo para a força agregada, o conflito não deve mutar silenciosamente um domínio inexistente. O consumidor responsável decide como aplicar o resultado.

---

## 64. Forced outcomes são autoridade externa limitada

**DECIDIDO**

Outcome constraints podem forçar, onde suportado:

- vencedor;
- outcome geral;
- ferimento;
- morte/vida;
- disposição.

Mas autoridade externa não quebra invariantes estruturais.

---

# Parte XV — Guerra, diplomacia, território e conflito amplo

## 65. Hostility, Conflict, War e Battle são conceitos distintos

**DECIDIDO**

```text
HOSTILITY != CONFLICT != WAR != BATTLE
BATTLE RESULT != WAR RESULT
```

Hostility é uma atitude ou relação negativa. Ela não implica que exista um
conflito ativo.

Conflict é uma oposição persistente entre dois ou mais lados. Pode existir
sem violência e pode se manifestar por recusas de contato, insultos, brigas,
homicídios, ataques, batalhas ou guerra. Os lados podem ser definidos pelo
domínio concreto; nenhum deles precisa ser uma `Polity`.

War é um conflito violento sustentado em escala coletiva significativa. Não
exige declaração formal, não exige uma Polity e pode envolver facções,
instituições, coalizões, forças rebeldes, grupos armados ou grupos de
criaturas quando essa for a semântica adequada ao mundo.

Battle é um episódio concreto de combate organizado dentro de um conflito ou
de uma guerra. Uma guerra pode passar dias sem batalha, e o resultado de uma
batalha não é automaticamente o resultado da guerra. Um Conflict mais amplo
também pode continuar depois que uma War termina.

```text
WAR != BATTLE
BATTLE != TERRITORIAL CONTROL
TERRITORIAL CONTROL != LEGAL OWNERSHIP/JURISDICTION
```

Essa distinção impede que uma hostilidade vire guerra por conveniência da
implementação ou que toda disputa política seja convertida automaticamente em
conflito armado.

Conflitos e guerras podem ter dois ou mais lados. Participar de uma guerra não
exige transformar todos os participantes em um `PoliticalActor` universal:
Faction, Institution, Polity, coalizão, força rebelde, grupo armado ou outro
ator coletivo adequado podem participar conforme o domínio.

---

## 66. Forças armadas são agregadas e possuem continuidade própria

**DECIDIDO / DIREÇÃO**

Uma Armed Force é uma estrutura militar hierárquica e agregada. Ela pode
conter contingentes agregados, Persons relevantes e subforces. A hierarquia é
genérica:

```text
Armed Force
└── Armed Force
    └── Armed Force
```

O conteúdo pode representar um exército, legião, horda, warband, pack, swarm,
frota ou host sem congelar uma hierarquia humana/moderna como contrato
universal. Uma força de milhares de indivíduos não exige milhares de `Person`
ou `NpcRuntime`; um Person nomeado pode ser um comandante, oficial, herói ou
outro indivíduo cuja identidade seja relevante.

```text
PHYSICAL SEPARATION != ORGANIZATIONAL SEPARATION
SEPARATE MISSION != NEW FORCE IDENTITY
```

Uma subforce pode operar separada da força principal por tempo significativo e
continuar pertencendo a ela. Durante o destacamento, pode ter localização,
comandante, ordens, suprimento, batalhas e casualties próprios. O retorno à
força principal não exige uma ruptura de identidade.

### Posição física da ArmedForce

Uma `ArmedForce` pode possuir zero ou uma posição física operacional atual no
primeiro modelo. Essa posição representa a presença física da própria força e,
quando houver posição, de seus contingentes diretos tratados como um corpo
co-localizado. Ela não é inferida da hierarquia e não representa
automaticamente o comandante, o headquarters, a parent force, os descendants,
o aggregate organizacional, um plano de movimento ou uma localização histórica:

```text
ARMED FORCE POSITION
!= COMMANDER POSITION
!= HEADQUARTERS POSITION
!= DESCENDANT POSITION
!= ORGANIZATIONAL AGGREGATE
!= MOVEMENT PLAN
```

`Position = null` é válido para uma força distribuída, para uma força sem
composição direta co-localizada, para um nível predominantemente de comando ou
quando a posição factual ainda não foi estabelecida pela autoridade espacial.
Uma parent force sem contingentes diretos não recebe a posição de seus filhos,
e a posição de uma parent não se propaga automaticamente para eles.

Na primeira representação, contingentes diretos de uma mesma força são
considerados co-localizados quando a força possui posição. Se a composição
precisar operar simultaneamente em locais diferentes, a direção preferida é
representá-la por subforces ou detachments com identidades próprias. Múltiplas
presenças físicas para uma mesma `ArmedForce` permanecem deferidas até existir
um consumidor concreto que exija essa semântica.

A posição física pertence semanticamente ao domínio de `ArmedForce`, mas não
precisa ser um campo da identidade ou da composição da força. A separação
conceitual é:

```text
ArmedForceStore
  → identity, hierarchy, composition, lifecycle, commander/relevant persons

ArmedForceSpatialState
  → current physical position
```

`SpatialAuthorityStore` continua sendo a autoridade dos lugares físicos e da
resolução de `SpatialReference`; ele não se torna a autoridade genérica de
todos os objetos móveis. O estado espacial de uma força deve validar sua
referência contra essa autoridade.

`OperationalLocationReference`, quando existir como string legada, é apenas
ponte transitória. Não pode permanecer como uma segunda autoridade depois que
uma posição tipada existir. Valores legados só devem ser convertidos quando a
conversão for inequívoca; valores não convertíveis não se tornam verdade
espacial silenciosamente.

Destacamento e posição são mudanças diferentes:

```text
DETACH != MOVE
REATTACH != ARRIVAL
POSITION STATE != MOVEMENT PLAN != MOVEMENT EXECUTION
```

Uma operação futura pode compor `Detach` e `SetPosition`, mas `Detach` não
deve movimentar implicitamente a força, e `Reattach` não deve levá-la à posição
da parent. Parent e child podem estar fisicamente separados sem que
`IsDetached` seja verdadeiro; esse estado representa separação operacional da
estrutura, não distância espacial nem uma nova identidade organizacional.

É importante distinguir:

- **subforce** — parte estrutural de outra força;
- **detached subforce** — continua subordinada, mas opera separadamente;
- **independent force** — possui identidade operacional própria sem a
  subordinação estrutural anterior.

Destacamento, reorganização, secessão, verdadeiro cisma, absorção e merger
genuíno não são a mesma transição. Destacamento e reorganização ordinária não
criam identidade nova automaticamente. Em uma secessão, a força original
preserva seu ID quando sua continuidade organizacional é clara, enquanto a
parte que rompe recebe nova identidade. Em um verdadeiro schism, no qual
nenhuma sucessora representa claramente a continuidade da força anterior, a
força antiga termina e todas as sucessoras recebem novas identidades.
Absorption pode preservar a identidade da força absorvedora e encerra a
absorvida; um merger genuíno encerra as predecessoras e cria uma identidade
nova.

Continuidade não deve ser decidida apenas pelo maior número de soldados, por
quem reteve o comandante anterior ou por conveniência da implementação.
Continuidade alegada também não é, por si só, identidade factual.

Comando, lealdade, pertencimento e controle não são equivalentes:

```text
COMMAND != LOYALTY != ALLEGIANCE
        != FORCE MEMBERSHIP != FUNDING != CONTROL
```

Uma cadeia de comando válida implica obediência normal. Não se deve avaliar
rebelião para cada ordem cotidiana. Desobediência, mutiny, desertion,
defection ou schism exigem causas semanticamente relevantes, como allegiance
conflitante, lealdade a outro comandante, falta de pagamento, grievance,
ideologia, medo, relações ou conflito político. A mudança de lado de um
comandante não transfere automaticamente seus subordinados: contingentes e
subforces podem segui-lo, permanecer leais, recusar ambos, desertar ou
defectar segundo as regras apropriadas.

---

## 67. Composição, mobilização, logística e capacidade militar

**DECIDIDO / DIREÇÃO**

Uma Armed Force não é apenas um número de manpower. Seus contingentes
agregados preservam, conforme a relevância do domínio, quantidade, origem ou
provenance, tipo de recrutamento/serviço e características militarmente
relevantes.

```text
MANPOWER SOURCE != ALLEGIANCE != LOYALTY != COMMAND
```

Uma fonte pode ser uma população agregada, um pool profissional, levy,
voluntários, mercenários, tropas vassalas, entidades convocadas, mortos
erguidos, unidades construídas, animais ou outro conteúdo adequado. Não se
deve assumir que toda força deriva de uma população civil humana. Essas
possibilidades não compartilham automaticamente a semântica de pessoal vivo:
fontes não vivas exigem accounting próprio e não são incluídas por antecipação
na primeira fundação de manpower vivo.

Recruitment e mobilization alteram a representação de indivíduos ou recursos;
não criam população factual. Quando uma fonte real fornece indivíduos para
uma força, eles continuam pertencendo à população do mundo e não podem ser
contados duas vezes. A arquitetura deve preservar, ainda que sem fixar esses
nomes como campos literais, a diferença entre população total, fonte civil ou
disponível e população mobilizada. Sobreviventes podem retornar à fonte ao
serem desmobilizados.

Para um contingent agregado, `Amount` representa o número atual de pessoal
vivo atribuído ao seu roster. Não representa, por si só, a quantidade
atualmente disponível para combate, a prontidão do pessoal, o total originalmente
recrutado nem a quantidade remanescente na fonte de origem.
`ContingentOriginReference` é provenance/content metadata; não vincula
automaticamente o contingent a uma fonte autoritativa de manpower.

Disponibilidade permanece separada do tamanho do roster. Feridos e capturados
continuam vivos e podem permanecer atribuídos ao contingent, embora
indisponíveis. A primeira fundação representa esse estado por coortes vivas
agregadas e disjuntas; a forma detalhada está definida abaixo. Desertion,
defection e demobilization, quando efetivamente aplicadas, removem pessoal do
roster e podem reduzir `Amount` sem implicar morte.

Os estados de casualty também não devem ser colapsados:

```text
DEAD != WOUNDED != CAPTURED != DESERTED
      != DEFECTED != DEMOBILIZED
```

Death é perda populacional permanente. Wounded continua vivo e pode estar
temporária ou permanentemente indisponível. Captured continua vivo sob a
custody de outro ator apropriado. Deserted abandona sua força sem implicar
mudança de lado; defected abandona uma força ou lado e adere a outro.
Demobilized/survivor pode retornar à origem. No contrato de `Amount`, morte
reduz o roster vivo; ferimento ou captura, por si sós, reduzem disponibilidade,
não `Amount`. Captura exige estado explícito de custody antes de ser afirmada
como verdade autoritativa. Esses efeitos agregados podem ser representados sem
materializar milhares de Persons. A morte de manpower ligado a uma fonte
populacional exige que a consequência factual correspondente para essa fonte
seja planejada segundo a semântica do domínio de origem; não se infere a fonte
a partir de provenance aberta. Planejar a consequência da fonte não muta seu
estado. Persons relevantes só recebem consequências individuais quando fatos
explícitos de participação ou exposição sustentarem a transição. Não há regra
proporcional universal de casualties.

### Fonte autoritativa e alocação

Um contingent pode ter zero ou uma binding autoritativa de manpower source.
Quando a contabilização de origens diferentes importa, a primeira forma é
representá-las em contingents distintos; múltiplas fontes autoritativas dentro
de um mesmo contingent ficam deferidas. A binding usa uma identidade estável e
tipada de fonte (`ManpowerSourceId`), resolvida pelo domínio dono da fonte.
`ContingentOriginReference` permanece metadata de conteúdo/provenance:

```text
ORIGIN / PROVENANCE != AUTHORITATIVE MANPOWER SOURCE
```

O domínio de origem continua autoridade sobre seu estado factual vivo ou de
recursos. O domínio militar possui bindings, alocação militar, roster e
disponibilidade, sem duplicar o total factual da fonte como outra verdade
mutável. A fonte e sua policy devem fornecer explicitamente quanto pode ser
alocado para uso militar. População viva não significa automaticamente
manpower recrutável, e `Settlement.CurrentPopulation` não é uma fórmula de
recrutamento nem um pool elegível universal.

A alocação por fonte é derivada dos rosters vivos autoritativamente ligados,
em vez de ser um segundo total mutável sujeito a divergência. Índices e caches
podem acelerá-la, mas são reconstruíveis e não autoritativos. Mobilização
atribui pessoal vivo à força sem removê-lo da população factual; a mesma
capacidade da fonte não pode ser alocada repetidamente além do limite que ela
permite. Para fontes populacionais, manpower militar alocado não excede nem a
capacidade militar explicitamente oferecida nem a população viva factual.

### Planejamento de consequências na fonte

A fundação de snapshots de source não autoriza, por si só, uma mudança factual
na fonte. Uma consequência que afete a fonte parte da identidade autoritativa
`ManpowerSourceId` e dos fatos atuais exatos dessa fonte, e produz uma proposta
imutável e específica ao domínio. A proposta não é a mutação: planejamento não
altera fonte, roster, Battle ou lifecycle.

A resolução de `ManpowerSourceId` para uma autoridade de consequência é uma
composição explícita do mundo. Não se descobrem tipos de fonte por parsing de
IDs, provenance, tipo de serviço ou nomes de exibição. Aliases não podem
representar silenciosamente o mesmo pool factual como fontes independentes.
Cada autoridade de planejamento possui identidade semântica estável de regra e
configuração (`RuleKey` e configuração efetiva), e só aceita as fontes e
efeitos registrados para ela; fonte ou efeito não suportado falha
explicitamente. Uma fonte futura pode declarar explicitamente que, por sua
própria semântica, um efeito não exige mutação externa separada. A ausência de
uma autoridade compatível nunca equivale a essa declaração.

A primeira aplicação populacional concreta usa `SettlementPopulationRuntime`
por associação explícita entre `ManpowerSourceId` e o settlement exato. A
capacidade militar alocável continua sendo uma entrada explícita e distinta da
população factual; não é calculada como `CurrentPopulation`. Se útil para
manter uma única composição de fontes, a mesma associação explícita pode
alimentar também os snapshots de alocação.

Para mortes de população agregada, a proposta reutiliza a fronteira existente
de demografia agregada, em vez de criar uma segunda autoridade de mutação. A
proposta registra a identidade do settlement, revisão populacional esperada,
população anterior, piso de residentes representados, mortes e população
resultante. A quantidade militar permanece `long`; conversão para um domínio
populacional baseado em `int` é checked. Overflow, underflow ou tentativa de
ultrapassar o limite de residentes representados rejeita a proposta inteira,
sem truncar, limitar nem aplicar lotes parciais.

O piso de residentes representados é factual e protegido: Persons
individualizados não são implicitamente parte de `Contingent.Amount`, então
mortes agregadas não podem reduzir a população abaixo desse piso. O valor deve
vir de uma consulta atual ou autoridade composta já responsável por essa
invariante, e a proposta captura o piso e suas dependências relevantes. Não se
faz descoberta de Persons dentro da regra de consequência nem se confia em um
inteiro cacheado obsoleto. A dependência de uma proposta de morte é estreita:
identidade da fonte, estado/revisão da população factual, piso representado e
identidade/configuração da regra de fonte. A fingerprint ampla usada para
alocação pode incluir capacidade e população factual, mas não é automaticamente
a única dependência da proposta de morte. Uma mudança apenas na capacidade de
recrutamento não a torna stale quando a capacidade não participou da proposta;
mudanças em fontes não relacionadas também não a invalidam.

### Roster vivo, disponibilidade e coortes

O estado mutável de manpower é conceitualmente separado da identidade
estrutural de Armed Force e Contingent e é associado a `ContingentId`. O
`Amount` semântico continua sendo o número atual de pessoas vivas atribuídas
ao roster, mas sua autoridade é a soma checked das coortes vivas daquele
contingent. `AvailableAmount` também é derivado — soma apenas coortes marcadas
como disponíveis — e não é um escalar mutável concorrente:

```text
Amount = SUM(living cohorts)
AvailableAmount = SUM(cohorts where availability is Available)
LIVING != AVAILABLE
```

As coortes são agregadas, mutuamente disjuntas e identificadas pelo estado
semântico combinado, inicialmente incluindo:

- ferimento: saudável ou ferido;
- custódia: livre ou capturado sob custodian válido;
- disponibilidade: disponível ou indisponível.

Combinar os estados permite, por exemplo, representar pessoal ferido e
capturado sem contá-lo duas vezes. Ferimento não implica, por si só,
disponibilidade nem indisponibilidade; isso é estado explícito. Capturados são
indisponíveis para o contingent original. Coortes com a mesma chave semântica
são mescladas e ordenadas deterministicamente. Não há `CohortId` ou identidade
de linhagem na primeira forma: identidade de estado atual não é histórico de
transições.

Se `ContingentRecord.Amount` permanecer durante uma migração incremental, será
apenas uma projeção de compatibilidade do estado de manpower e deverá ser
igual à soma das coortes. Nenhuma operação normal pode alterá-lo
independentemente; novos consumidores usam a autoridade de manpower. O caminho
normal de substituição de contingent pode mudar metadata estrutural permitida,
mas preserva o roster. Registro estrutural não cria arbitrariamente roster
positivo sem alocação correspondente. Bootstrap/importação pode ter caminho
distinto e explícito.

Contingents positivos legados sem binding continuam válidos. Seu estado inicial
pode ser normalizado para saudável, livre, disponível e sem fonte vinculada,
preservando a semântica de combate existente sem inventar uma origem
retroativamente. Podem continuar participando da execução D3/D4/D5, mas não
podem produzir operações autoritativas que dependam de uma fonte desconhecida,
como morte agregada debitada dessa fonte ou retorno por desmobilização, até
serem vinculados ou reconciliados explicitamente. Contingent com roster zero
também é válido; sua identidade pode permanecer, mas coortes e alocação somam
zero.

### Transições, custody e conservação

A primeira autoridade de custody agregada é uma `ArmedForceId` válida; uma
`BattleSideId` não é custodian durável. Captura mantém pessoal vivo e atribuído
ao roster original, mas indisponível, e registra o custodian. Sem custodian
suportado, captura não pode ser afirmada como verdade autoritativa. Outros
tipos de custodian aguardam consumidores concretos.

Morte remove manpower vivo das coortes e reduz `Amount`; se a fonte autoritativa
representa população viva, a proposta da mesma operação inclui a transição
correspondente do domínio de origem, a ser aplicada conjuntamente depois.
Ferimento não reduz `Amount`; qualquer mudança de disponibilidade é explícita.
Captura não reduz `Amount`. Desmobilização remove a alocação do roster e pode
devolver sobreviventes à fonte conforme as regras dela, sem aumentar população
que já estava viva. Desertion e defection também exigem transições explícitas,
sem serem inferidas de derrota ou tratadas como casualties automáticas.

Nenhuma alocação cria pessoal; nenhum membro desaparece sem morte, saída,
transferência ou outra transição modelada. Quantidades genéricas de manpower
permanecem `long`. Adaptadores para domínios populacionais baseados em `int`
usam conversão checked, validam limites e verificam todas as precondições
antes de qualquer mutação; não truncam, limitam silenciosamente nem aplicam
lotes parciais.

Persons nomeadas explicitamente referenciadas por uma força não são
implicitamente contadas em `Contingent.Amount`:

```text
PERSON REPRESENTATION != AGGREGATE ROSTER REPRESENTATION
```

`ArmedForcePersonReference` por si só não prova membership no roster agregado.
Se uma Person vier a integrar um contingent, a atribuição `PersonId` →
`ContingentId` e suas regras de accounting devem ser explícitas antes de
consequências individuais poderem alterar o roster.

Terminar uma força não é morrer nem ser desmobilizado. Uma força com roster
vivo direto não termina antes da disposição explícita desse roster por
transferência, desmobilização ou outra transição adequada. Se for custodian de
manpower agregado capturado, custody também precisa ser transferida ou
encerrada antes do término. Roster de descendants não é tratado como roster
direto do parent. A remoção de uma fonte também não faz desaparecer pessoal
militar vivo já alocado: ela pode deixar de aceitar novas operações enquanto
permanece referenciada até reconciliação explícita. O lifecycle completo de
fontes continua aberto.

Vitória ou derrota não determina, por si só, consequências de manpower. A
fundação de roster e disponibilidade não define uma regra de casualties, não
converte `LossFraction` em headcount e não consome RNG de casualty. Regras de
consequência devem declarar quais fatos produzem; seus efeitos diretos precisam
ser aplicados conjuntamente. Quando mortes planejadas em vários contingents
atingem a mesma `ManpowerSourceId`, D6B2 agrega os valores antes de produzir uma
única proposta D6B1 para essa source, preservando a rastreabilidade dos efeitos
originais. Derrotas não implicam retirada, rout ou surrender.

As necessidades logísticas devem ser derivadas da composição da força e não de
uma lista universal fixa de recursos. Uma força pode precisar de provisions,
alimentação animal, munição, manutenção, combustível arcano, sangue,
sustentação necromântica, materiais de reparo ou outros recursos.

```text
SUPPLY != FUNDING / PAY
```

Escassez produz consequências graduais em readiness, morale, cohesion,
movimento, capacidade de combate, saúde, desertion ou pressão por
requisition/foraging. Ela não deve ser reduzida a `ArmyEnabled = false`.
Foraging e requisition podem afetar economia, população e social appraisal,
mas essas consequências continuam dependendo das regras, decisões e fatos
dos domínios envolvidos.

Morale, cohesion e loyalty também permanecem distintos:

- **morale** é disposição ou confiança para continuar lutando;
- **cohesion** é capacidade de funcionar como unidade organizada;
- **loyalty** é disposição de seguir pessoa, entidade ou causa.

Não existe `ArmyStrength` como verdade causal universal primária. Military
capability depende de composição, equipamento, readiness, supply, morale,
cohesion, comandante, terreno, conhecimento, plano/tática e fatores
situacionais. Qualidade de comandante não é um bônus genérico `+X`; capacidades
como defesa, manobra, emboscada ou cerco podem importar em contextos
diferentes.

Decisões militares seguem a cadeia geral:

```text
KNOWLEDGE → INTERPRETATION → PLAN / DECISION → ACTION
```

O comandante decide com o conhecimento disponível. Não conhece magicamente
manpower, morale, posições, forças ocultas, rotas ou planos inimigos exatos.
Scouts e spies podem produzir conhecimento que permita escolher uma rota ou
plano, mas a execução revalida a World Truth. Isso não introduz, por si só,
`WarAI` ou um `Intelligence Engine` universal.

---

## 68. Movimento militar, batalha, controle e término de guerra

**DECIDIDO / DIREÇÃO**

Military movement possui semântica própria. Pode reutilizar a verdade
geográfica e de localização de Travel, mas não é ordinary Travel. Tamanho e
composição da força, terreno, rotas conhecidas, scouts, logística, baggage,
weather, presença inimiga e segurança podem alterar a movimentação. Uma força
não atravessa magicamente uma rota desconhecida; exploração à frente pode
produzir knowledge para uma decisão posterior.

É válido afirmar que uma força está em uma `SpatialReference` sem representar
como chegou ali. Estado de posição, plano de movimento e execução do movimento
não são a mesma coisa. A posição pode futuramente ser alterada por movimento,
comando externo, bootstrap de cenário ou consequência de outro domínio, mas a
existência da posição não cria por si só um `MilitaryMovement`.

Battle deve ser resolvida de forma agregada, sem requisito de combate golpe a
golpe ou turno a turno. O plano ou a tática escolhida com base em knowledge
influencia a interação entre capabilities, situação e terreno, mas não
garante o resultado. Uma batalha pode produzir casualties, ferimentos, morte
ou captura de comandante, withdrawal, rout, surrender, perdas de equipamento
ou supply, mudanças de morale/cohesion, mudança de posição e oportunidades de
alterar military control, quando regras explícitas estabelecerem esses fatos.
Possibilidades de aftermath não são consequências implícitas de Victory ou
Defeat, e referências a comandante não bastam para afirmar consequência
individual.

Uma Battle persistente e sua elegibilidade para execução são estados
distintos:

```text
BATTLE REGISTRATION/START != BATTLE EXECUTION ELIGIBILITY
```

Bindings de participantes não provam co-localização no momento do registro, e
`Pending → Active` não congela as posições das forças. Antes da execução, um
contexto de Battle deve revalidar a Battle ativa, sua localização, os
participantes, a posição física atual das forças, a compatibilidade espacial,
a composição direta e as revisões ou fingerprints das dependências realmente
usadas. Uma mudança posterior de posição torna um contexto capturado obsoleto,
mas não invalida retroativamente o registro persistente da Battle.

### Composição direta e participação operacional

Uma binding explícita de `ArmedForce` em uma Battle representa a participação
daquela força, não a inclusão automática de seus descendants:

```text
ArmedForce binding → direct contingents of that ArmedForce
```

O agregado organizacional continua sendo uma consulta estrutural e não deve
alimentar diretamente o manpower da Battle. Assim, se Army e Legion A forem
bindings explícitas, a Army contribui apenas com seus contingentes diretos e
a Legion A apenas com os seus. `ContingentId` estável deve permitir detectar
ou rejeitar duplicação durante a projeção de execução.

Uma parent force sem contingentes diretos pode permanecer registrada como
participante, representando envolvimento organizacional, presença de comando
ou intenção ainda não expandida. Porém, ao criar o contexto de execução, ela
contribui com zero combatants. Não se deve gerar manpower dos descendants por
shorthand implícito. Cada side precisa possuir pelo menos um combat element
explícito e utilizável antes de uma resolução; uma Battle persistente pode
existir sem ser imediatamente executável.

### Comando operacional temporário

Hierarquia organizacional e comando operacional são relações diferentes:

```text
ARMED FORCE PARENT != OPERATIONAL COMMAND RELATION
```

Forças podem atuar juntas em uma operação sem fundir suas identidades. O
primeiro modelo pode representar um comandante geral e as forças subordinadas
no plano ou contexto de execução da Battle, sem reparenting organizacional.
`Force Commander` não é automaticamente `Battle Side Commander`.

Não se introduz ainda uma entidade persistente `OperationalGroup`. Para o
primeiro execution slice, `BattleSide` mais bindings explícitas de
`ArmedForce` são suficientes. Uma estrutura operacional própria só deve surgir
quando houver consumidor real para múltiplos grupos no mesmo side, ordens ou
planos separados, logística, continuidade entre Battles ou diagnostics
próprios. Inicialmente, seu escopo preferencial seria a operação ou a Battle;
um grupo persistente de War exigiria justificativa adicional.

`CommanderPersonId`, officers e heroes são referências de identidade e papel.
Não implicam presença física, participação individual, manpower ou
co-localização. Enquanto não existir posição canônica de `Person`, estar na
posição X não significa que o commander está em X. Commanders podem ser
metadata de comando sem se tornarem named combat participants.

### Contexto de execução

A transição futura deve preservar três camadas distintas:

```text
PERSISTENT BATTLE STATE
    → BATTLE EXECUTION CONTEXT
        → BATTLE/CONFLICT OUTCOME
```

O contexto de execução deve capturar somente inputs relevantes e revalidáveis,
como `BattleId`, fronteira lógica ou dia, localização física validada, sides
em ordem determinística, participantes explícitos, contingentes diretos,
`ContingentId` estáveis, dados de capability necessários, comando operacional
opcional e revisões ou fingerprints das dependências utilizadas. Sua criação
não deve consumir aleatoriedade autoritativa.

Um contexto fica stale quando muda uma dependência que ele realmente usa:
movimento da força ou alteração dos contingentes, por exemplo. Mudança de
comandante que não participa do cálculo não precisa invalidá-lo; se o comando
afeta o resultado, a mudança deve torná-lo stale. A validação deve preferir
revisões específicas ou fingerprints relevantes em vez de invalidar por toda e
qualquer alteração do mundo.

```text
EXECUTION CONTEXT != PERSISTENT STATE != OUTCOME != HISTORY
```

### Primeira fronteira de resolução bruta

**DECIDIDO / DIREÇÃO**

A primeira resolução de uma `Battle` deve atravessar uma fronteira estreita e
explicitamente efêmera:

```text
BattleExecutionContext
    → projeção determinística Battle-to-Conflict
        → resolução bruta lower-level
            → BattleResolutionComputation
```

Essa fronteira não transforma a resolução em consequência aplicada. A
distinção é:

```text
RESOLUTION COMPUTATION
    != APPLIED OUTCOME
    != PERSISTENT BATTLE RESULT
    != CONSEQUENCES
    != HISTORY
```

Na primeira fronteira, a `Battle` permanece `Active`. Não são aplicadas
casualties, não se altera `Contingent.Amount`, população ou posição, não se
cria aftermath, não se registram eventos ou history e não se marca uma
`PersistentBattle` como `Resolved`. O resultado bruto também não deve chamar
um caminho que combine resolução e aplicação de consequências.

O contexto precisa ser validado novamente antes de qualquer aleatoriedade
autoritativa. A ordem semântica é:

```text
validate current context
    → project deterministic capability
        → project deterministic lower-level conflict
            → consume contextual authoritative randomness
                → compute raw result
```

Contexto stale ou inválido produz ausência de resolução, sem mutação e sem
consumo de RNG autoritativo. A projeção de capability é pura, determinística e
não consome aleatoriedade. Ela só pode usar os inputs capturados ou derivados
do boundary de execução; um fato adicional só pode tornar-se causal depois de
entrar explicitamente nesse boundary e na sua semântica de stale/fingerprint.

### Capability específica da resolução

Capability de resolução de Battle é uma projeção contextual, não uma nova
verdade militar persistente:

```text
BATTLE RESOLUTION CAPABILITY
    != ARMED FORCE STRENGTH
    != ARMY STRENGTH UNIVERSAL
    != PERSISTENT WORLD FACT
```

A fronteira deve receber uma regra de capability de Battle explicitamente
fornecida, determinística, pura, sem RNG autoritativo e sem mutação. Não existe
uma fórmula militar default implícita nessa primeira resolução: uma produção
real exige um provider/regra de capability compatível, enquanto testes podem
usar uma regra determinística fixa. Não se deve transformar automaticamente
`Contingent.Amount` em capability nem introduzir uma fórmula `Amount ×
Quality` sem uma decisão de domínio.

A identidade semântica da regra e da sua configuração participa da identidade
causal da resolução. Providers que possam produzir projeções diferentes não
podem compartilhar a mesma identidade de RNG apenas por terem o mesmo tipo ou
por coincidência de identidade de objeto no host.

### Granularidade e mapeamento

Cada `ContingentId` direto capturado no contexto projeta exatamente um
participante agregado da resolução lower-level. Não se deve colapsar uma força
inteira, uma side inteira ou Persons individuais em substituição dessa
granularidade. Isso preserva heterogeneidade, rastreabilidade e uma futura
aplicação de consequências sem transformar o lower-level em autoridade de
Battle.

O mapeamento permanece tipado e imutável:

```text
lower-level participant
    ↔ BattleId ↔ BattleSideId ↔ ArmedForceId ↔ ContingentId
```

IDs lower-level devem ser namespaced e determinísticos. Não podem depender de
allocator sequencial, Unity ID, display name ou ordem de inserção. O
`Contingent.Amount` continua sendo `long` no snapshot autoritativo; a primeira
projeção não o converte, trunca ou substitui por um `Count` menor. Se a
foundation lower-level ainda possui um campo incompatível, a contagem não deve
ser usada como substituto silencioso do amount.

`Amount > 0` e capability positiva são conceitos distintos. Um contingente
pode ter amount positivo e capability zero. A primeira resolução não cria uma
regra adicional de elegibilidade baseada em capability positiva; se todos os
lados resultarem em capability zero, permanece a semântica bruta da foundation
lower-level, inclusive um possível `Draw`.

A elegibilidade D3 atual usa `Contingent.Amount > 0` como elemento explícito de
combate, o que é adequado antes da fundação detalhada de disponibilidade. Com
essa fundação, elegibilidade usa `AvailableAmount > 0`, não o roster vivo
total. O contexto captura os fatos de disponibilidade causalmente necessários:
uma mudança relevante torna-o stale, enquanto mudanças não relacionadas não
o invalidam.

A projeção de capability D4 recebe o estado factual disponível capturado como
input de execução; ela não define `capability = AvailableAmount`. A regra
autorizada de capability continua determinando como manpower disponível e
outros fatos afetam capability, e pessoal indisponível não contribui
silenciosamente como se estivesse pronto para combate. D5 reconstrói D3 e
recomputa D4; mudanças relevantes de disponibilidade tornam obsoleto um plano
anterior. Binding ou metadata de fonte só entra na causal fingerprint bruta
quando uma regra efetivamente a consome.

### Identidade causal e aleatoriedade

A identidade do `BattleExecutionContext` não é automaticamente a identidade
causal de uma resolução. A causal fingerprint deve conter apenas dados que
participam da projeção ou do resultado, incluindo semanticamente a Battle, o
dia/fronteira de execução, a localização factual quando usada pela regra de
capability, as sides e forces ordenadas, os contingentes ordenados, seus dados
causais, as capabilities projetadas e a identidade/configuração da regra de
capability.

Metadata de commander permanece não causal enquanto não modificar capability
ou resultado. A presença de um commander não cria bônus, altera a chave de
RNG ou muda o vencedor. Se uma regra futura usar comando causalmente, isso
deverá ser explicitado e refletido na fingerprint.

A aleatoriedade da resolução deve ser contextual e determinística. A operação
deve derivar sua identidade da fingerprint causal e da side correspondente,
sem usar `UnityEngine.Random`, `System.Random` ad hoc, stream sequencial
compartilhado com operações não relacionadas ou ordem incidental de coleções.
Assim, os mesmos inputs causais, a mesma regra de capability e a mesma
autoridade determinística de aleatoriedade produzem o mesmo resultado bruto,
sem que uma resolução não relacionada altere o resultado por ter consumido
RNG antes.

A primeira fronteira não expõe constraints de outcome ou consequências de
participantes. Um `GM ForceOutcome` permanece fora dela enquanto o caminho
lower-level não oferecer uma forma não causal de aplicar essa autoridade.
Também não há ainda objective ou stakes militares autoritativos. Placeholders
lower-level transitórios, se necessários para atravessar a foundation, não
representam objetivo, stakes ou semântica militar e não podem alimentar um
resolver de consequências como se fossem fatos do domínio.

Não se criam modifiers para terrain, commander, tactics, morale, cohesion,
readiness, supply ou logistics sem regras e fatos explícitos desses domínios.
A localização autoritativa continua sendo a `SpatialReference` tipada do
contexto; ela não é rebaixada a um identificador de localização lower-level
nem cria uma segunda autoridade espacial.

### Computação efêmera e fronteira posterior

`BattleResolutionComputation` é um wrapper Battle-specific efêmero. Ele pode
preservar a identidade da Battle, o dia de execução, a fingerprint do contexto,
a causal fingerprint da resolução, o identificador lower-level adaptado, o
resultado bruto e os mapeamentos tipados de sides/participants, além da
identidade da regra utilizada. Não é `BattleOutcome`, `WarResult`, evento,
history, schema de save ou aplicação de consequência.

O resultado lower-level permanece apenas resultado bruto: `Victory`/`Draw` e
suas disposições são dados da computação, não fatos militares persistentes.
Retreat, rout, surrender, capture, completion de objetivo, controle territorial
e outros resultados militares não devem ser inventados nessa camada.

O identificador do conflito adaptado não é automaticamente o identificador de
um `Conflict` persistente. Ele deve ser namespaced, determinístico e derivado
da Battle, da fronteira de execução e da identidade causal da resolução.

Se uma aplicação futura usar essa computação depois de outra mutação, deve
revalidar o contexto e suas dependências. Uma computação stale é rejeitada e
deve ser refeita a partir de um novo contexto; o raw result não é reutilizado
automaticamente sobre estado antigo.

O uso atual de números `float` pode ser suficiente para a primeira computação
bruta no runtime suportado. Isso não encerra a política de determinismo
numérico autoritativo entre hosts ou em um futuro `Simulation.Core`. Antes de
aceitar ou persistir um outcome de Battle, deve existir um gate próprio para
essa política; não se deve refatorar a aritmética especulativamente apenas
para iniciar a fronteira bruta.

A transição `Active → Resolved` permanece fora desta primeira resolução. Uma
computação D4 válida ainda não é, por si só, um outcome autorizado do mundo.
Uma regra identificada pelo caller, fingerprints coerentes ou settings
consistentes não provam autorização: uma computação criada com dependências
arbitrárias não pode alterar World Truth.

### Autorização e outcome terminal da Battle

A futura aceitação autoritativa deve ser vinculada à policy efetiva do mundo e
ao boundary de composição. A policy de resolução seleciona regras/configurações
de capability, autoridade/configuração de aleatoriedade, settings do resolver,
versão da projeção e perfil numérico suportado. A policy de consequências
diretas é uma autoridade efetiva separada, composta pelo mundo e identificada
com suas próprias regras, versão e dependências. A identidade textual `RuleKey`
fornecida por um caller não substitui nenhuma dessas autoridades; authoring,
resolução efetiva e consumo pelo domínio permanecem etapas distintas.

A operação de aplicação deve partir da `BattleId` ou de um pedido de resolução:
o mundo constrói o contexto atual, recalcula sob a policy autorizada, deriva o
outcome semântico e o plano de consequências, revalida as dependências e então
faz o commit. Uma computação externa pode servir de preview ou proposta; uma
fingerprint esperada pode registrar a aprovação humana. Na aplicação, o mundo
recalcula com a policy atual. Se o contexto ou a fingerprint diferir, a
proposta é rejeitada ou requer nova confirmação, em vez de se confiar no objeto
computado pelo caller.

O primeiro modelo permite no máximo um outcome terminal aceito por
`PersistentBattle`. A Battle pode permanecer `Active` por vários dias, mas sua
resolução aceita a encerra. Não se reutiliza a mesma Battle terminal para
rounds ou engagements repetidos; se isso se tornar necessário, cada processo
precisará de identidade e estado explícitos.

`ConflictResolutionResult != BattleOutcome`. O primeiro `BattleOutcome`
persistente afirma apenas `Victory`, com o `WinningBattleSideId`, ou `Draw`, sem
vencedor, além do dia lógico de resolução e provenance mínima da resolução
autorizada. Não afirma automaticamente vencedor ArmedForce, objetivo cumprido,
retirada, rout, surrender, captura, deslocamento, controle territorial ou
resultado de War. Capability projetada, fator aleatório, score lower-level e
breakdown de floats não são World Truth; podem ser mantidos como diagnostics
ou provenance quando necessário.

O outcome preserva somente a provenance necessária para identificar a Battle,
o dia lógico aceito, a policy/perfil autorizado, a identidade causal da
computação e as versões de projeção/regra relevantes. IDs lower-level de
Conflict não são autoridade nem identidade persistente da Battle. No primeiro
modelo, uma Battle admite zero ou um outcome terminal, tornando a própria
`BattleId` suficiente como raiz de idempotência.

### Plano de consequências e aplicação atômica

Um outcome não pode ser aceito com consequências diretas omitidas sob a
justificativa de que um domínio declarado pela policy ainda não existe. A
policy deve cobrir explicitamente cada efeito direto que afirma produzir. Uma
policy configurada pode declarar zero efeitos; ausência de policy, planner de
source necessário para um efeito proposto, modelo necessário ou planejamento
bem-sucedido não significa zero. Policy não configurada, planner de source
necessário ausente, modelo de consequência não fornecido e falha de
planejamento permanecem resultados distintos de uma consequência zero
completa. Uma coleção vazia não pode representar ambiguamente ambos.
Consequências agregadas de forças não se convertem em consequências de Persons
sem fatos explícitos de participação e exposição individual.

### Boundary de planejamento direto: D6B1 e D6B2

D6B1 planeja consequências de fonte, sem aplicá-las. D6B2 planeja
consequências diretas de Battle e também não muta World Truth. São fronteiras
distintas: a primeira traduz um efeito sobre uma fonte autoritativa em uma
proposta do domínio dono dessa fonte; a segunda combina o outcome autorizado
com fatos militares atuais e a policy de consequência do mundo.

D6B2 começa por `BattleId` e estado atual do mundo. Obtém ou reconstrói
internamente um plano D5 autorizado e fresco, cuja validade corrente é
confirmada antes do planejamento. O caller pode fornecer somente metadata de
plano de execução que D5 legitimamente aceite; outcome, computação D4, regra de
consequência e planner de source são autoridades compostas pelo mundo, não pelo
pedido. Um plano/outcome arbitrário fornecido por caller não é autoridade.
Preview externa não substitui a recomposição autorizada. D6B2 v1 produz o plano
corrente e permite validá-lo como current/stale, sem exigir uma fingerprint
esperada de consequência. Se um consumidor futuro precisar de confirmação
humana de preview, essa precondição pertence ao boundary desse consumidor.

A policy de consequências diretas é distinta da policy de resolução bruta. Ela
identifica a regra efetiva, seu `RuleKey`, identidade e versão de configuração,
a versão de plano/coverage e as autoridades de planejamento de source
suportadas. A identidade semântica dessa composição participa da validação do
plano. Na v1, a fingerprint da policy não inclui identidade de RNG, pois D6B2
não tem aleatoriedade de consequência. A ausência de policy ou regra necessária
torna o planejamento indisponível/falho; não autoriza fallback implícito.

Não existe casualty formula default como `dead = Amount × LossFraction`, nem
`DefaultConflictConsequenceResolver` é autoridade para converter perda de
capability em headcount militar. A regra não pode ser escolhida livremente pelo
caller em cada pedido. D6B2 v1 fornece a fronteira de policy/regra, não uma
fórmula de casualties de produção; regras determinísticas de fixture pertencem
aos testes, enquanto uma regra efetiva de jogo deverá ser composta
explicitamente. Fixtures podem cobrir zero completo, partições fixas, wound,
capture, mortes multi-source e saídas inválidas sem se tornarem defaults de
produção.

D6B2 v1 avalia a regra autoritativa **uma vez por Battle**, recebendo a coleção
canônica de todas as coortes expostas daquela Battle. A regra não é invocada
independentemente por coorte na fronteira autoritativa, embora possa calcular
partições individuais internamente. O escopo único permite que uma regra
coordene sides, captura e custódia em Battles com duas ou mais sides e produza
uma resposta globalmente completa.

O input da regra é uma captura imutável dos fatos explicitamente necessários:
`BattleId`, dia lógico, `Victory` com vencedor ou `Draw`, provenance/identidade
causal D5, sides da Battle, forças participantes, contingents diretos, coortes
expostas D3/D6A, bindings de source e mapeamentos tipados de participante/side
necessários para validar custódia. A regra não recebe stores mutáveis nem
`SimulationRuntime`, não consulta o mundo ao vivo e não contém autoridade para
alterá-lo. Com o mesmo input capturado e a mesma regra/configuração imutável,
produz a mesma saída.

A primeira policy D6B2 usa somente o outcome semântico `Victory`/`Draw`, o
mapeamento tipado Battle → side → force → contingent, os fatos de execução D3,
as coortes D6A e fatos explícitos de fonte/custody necessários. Ela não deriva
casualties de floats, capability totals ou fatores aleatórios brutos de D4.
Sua provenance causal D5 identifica qual resolução autorizada produziu esse
outcome. Isso não resolve a equivalência numérica cross-host de D4/D5, que
continua uma limitação explícita. A policy D6B2 v1 não introduz nova semântica de
consequência em ponto flutuante nem expõe à regra scores brutos D4, totais de
capability, random swing factors ou dispositions lower-level de Conflict.
Versões futuras só poderão consumir fatos D4 adicionais após decisão
arquitetural explícita.

Somente participantes explícitos da Battle e seus contingents diretos podem
receber consequências diretas v1. Não se expandem efeitos a parent forces,
descendants, participantes de War/Conflict, aliados não vinculados ou forças
próximas. A exposição inicial é determinada pelas coortes capturadas como
`Available` no contexto D3 pertencente ao plano D5 fresco. Coortes
indisponíveis — inclusive capturados — não são alvo; coortes `Wounded` e
`Available` continuam expostas. Uma mudança de `Available` para outro estado
durante a resolução da regra não amplia a exposição inicial.

Para **cada** coorte de entrada exposta, a regra produz exatamente uma partição
explícita em zero ou mais coortes vivas de destino e uma quantidade terminal de
mortes. A partição identifica a entrada por side, force, contingent e estado
semântico da coorte; não inventa um `CohortId`. Cada destino declara injury,
custody, custodian quando capturado, availability e amount positivo. Destinos
são disjuntos: não se somam contadores independentes de feridos, capturados e
mortos que possam descrever as mesmas pessoas. `Dead` não é estado de coorte
viva nem valor de injury state; morte é saída terminal do roster. Uma entrada
omitida, duplicada, desconhecida ou parcialmente explicada invalida a saída; a
regra não tem permissão para considerar omissão como sobrevivência inalterada.
Transições são canonicalizadas e ordenadas por semântica estável de coorte,
nunca pela ordem incidental de collections. Para cada coorte exposta de
quantidade `N`:

```text
SUM(living destination cohorts) + terminal deaths = N
post-plan living roster = pre-plan living roster - planned deaths
```

Wound ou capture isolados não alteram o roster vivo. As somas usam aritmética
checked, quantidades não são negativas e todo o contingente exposto é explicado
sem criação, desaparecimento ou aplicação parcial. Um sobrevivente inalterado
deve ser listado explicitamente como destino igual ao estado semântico de
entrada, com zero mortes. Coortes não expostas não recebem transição da regra e
permanecem inalteradas; qualquer projeção pós-Battle deve carregá-las adiante.
As partições por coorte são a única verdade causal da proposta. Totais por
source e projeções pós-Battle são visões derivadas, não saídas autoritativas
independentes.

A coverage agregada D6B2 v1 limita-se às transições abaixo:

- input `Healthy + Available`: sobrevivente `Healthy`, sobrevivente `Wounded`,
  sobrevivente capturado `Healthy` ou `Wounded`, ou morte;
- input `Wounded + Available`: sobrevivente `Wounded`, sobrevivente capturado
  `Wounded`, ou morte.

Para sobreviventes não capturados, availability é sempre declarada
explicitamente. A injury state é binária: `Wounded → Wounded` não representa
novo ferimento nem severidade maior. A v1 não permite `Wounded → Healthy` nem
reinjury; recuperação pertence a outro processo. Se a regra exigir um estado
que o modelo não representa, o planejamento falha, sem inventar cura ou
gravidade e sem presumir que todo ferido fica indisponível.

Uma coorte capturada permanece viva no contingent e source originais, é
marcada como capturada e indisponível e exige um `ArmedForceId` custodian
explícito. No modelo inicial, o custodian precisa ser uma força participante
ativa da mesma Battle e estar em `BattleSideId` diferente do contingent
capturado, e é selecionado explicitamente pela regra. Não se infere custodian
do vencedor, da primeira força ou da primeira força da side vencedora. Captura
preserva `ContingentId` e `ManpowerSourceId`; não invoca D6B1 nem causa
transição populacional separada. O modelo admite mais de duas sides e `Draw`;
empate não implica zero casualties
ou capturas, vitória não implica ausência de perdas do vencedor nem captura
total do derrotado, e qualquer side pode ser afetada. Se a força custodian
terminar antes da aplicação, a proposta fica stale.
Não se converte a Battle em uma estrutura binária fixa de attacker/defender.

Antes de invocar D6B1, D6B2 soma, com aritmética checked, todas as mortes
planejadas de contingents/coortes vinculados à mesma `ManpowerSourceId`. Para
cada source afetada há exatamente uma solicitação agregada e uma proposta de
consequência de source; não se planejam várias transições independentes a
partir do mesmo snapshot ainda não aplicado. O plano preserva a trilha
coorte → contingent → mortes → source → total agregado → proposta D6B1, e D7
deve poder verificar que o total de mortes bound à source é igual ao efeito
proposto por D6B1. Wound, capture e morte zero não invocam D6B1.

Morte positiva em contingent ligado a uma source exige uma proposta compatível
da autoridade dessa source. A authority pode propor a mudança factual da fonte
ou declarar explicitamente, segundo a semântica daquela fonte, que não há
mutação externa adicional; a ausência de planner não equivale a essa
declaração. Em contingent sem source, morte positiva falha para todo o
planejamento — não se omite, converte em wound nem reduz roster sem accounting
de source. Se qualquer proposta D6B1 necessária falhar ou ficar stale, não há
plano D6B2 parcial ou completo retornado.

A validação conceitual segue a causalidade: obter e revalidar D5; capturar o
input imutável; validar a policy; executar uma vez a regra da Battle; validar
identidade, coverage, forma das partições — rejeitando entradas expostas
ausentes, duplicadas ou desconhecidas —, conservação e custódia; agrupar mortes
por source; planejar e validar uma proposta D6B1 por source afetada; e revalidar
D5, estado de manpower participante, policy, propostas e estado/participação/
side dos custodians antes de retornar o plano. Planejar e validar não
redistribui nem mata roster, não aplica transição populacional, não persiste
outcome e não muda lifecycle da Battle. Falhas normais — como policy ausente,
Battle/D5 inválido ou stale, falha da regra, partição incompleta/inválida,
violação de conservação, custody inválida, morte sem source ou falha/staleness
de proposta D6B1 — são resultados explícitos; nenhuma retorna um plano
parcialmente completo.

O plano D6B2 v1 cobre morte, wound e capture para todas as coortes expostas,
conforme a policy declarada; ele não afirma simular todo aftermath possível.
Desertion, defection, demobilization, retreat, rout, surrender, deslocamento
espacial, controle/occupation e casualties de Persons ficam fora dessa
cobertura. Victory/Draw não implica nenhuma dessas consequências ausentes.

D6B2 v1 é determinístico e não introduz contrato nem tipos de RNG para
consequências. A regra pura produz a mesma saída para o mesmo input capturado e
a mesma composição imutável. Isso não altera a regra geral: se uma versão
futura usar aleatoriedade de consequência, sua autoridade deverá ser
contextual, semanticamente keyed e separada do RNG D4, sem depender de ordem
de iteração ou de um `Next()` global. Essa extensão exige decisão arquitetural
própria.

O plano D6B2 é uma camada imutável separada do plano D5 e não o altera nem o
reutiliza como recipiente de efeitos. Captura BattleId/dia, outcome e
provenance/causalidade D5, fingerprint/versionamento de policy e coverage D6B,
fingerprint do contexto de execução, inputs expostos ordenados, partições
canônicas, morte por coorte/contingent, totais agrupados por source, propostas
D6B1, dependências de custódia e fingerprint do plano. Uma projeção pós-Battle
pode ser derivada deterministicamente do snapshot completo pré-Battle e das
partições: preserva coortes não expostas, inclui destinos vivos e remove mortes,
derivando roster vivo e disponibilidade. Não é escrita separada pela regra nem
uma segunda autoridade. Fingerprints do plano e de suas dependências usam
identidades e versões semânticas estáveis, nunca identidade de objeto,
`GetHashCode` ou endereço de runtime, e excluem revisions globais e mudanças
não relacionadas.

Uma única semântica de completeness, conceitualmente
`DirectConsequencesComplete`, basta. Ela prova que a regra foi executada,
todas as coortes expostas foram particionadas, conservação se mantém, cada
morte que exige source tem proposta válida e cada captura tem custódia válida.
Uma partição completa em que todos sobrevivem inalterados e todos os efeitos
são zero é válida; ausência de policy ou modelo nunca é zero completo.
`Consequence-complete` não significa `Commit-ready`: D6B2 não tem autoridade de
aplicação; somente D7 valida as autoridades de commit e decide se a transação
inteira pode ser aplicada.

A fronteira conceitual é:

```text
resolução atual autorizada
    → BattleOutcome semântico D5
        → plano imutável de consequências diretas D6B2
            → propostas de transição de fonte D6B1 quando necessárias
                → revalidação de dependências e policy
                    → aplicação lógica atômica D7
                        → tentativa de evento/history
```

Criar ou validar o plano não muta o mundo. A validação D6B2 cobre o dia lógico
vigente, outcome e contexto D5, policy/fingerprint de consequência, coortes
participantes, propostas e dependências de source e elegibilidade/lifecycle de
custodians, incluindo participação e side. Uma mudança em qualquer dependência
efetivamente usada torna o plano stale; mudanças em forças,
sources, Battles ou coortes fora das dependências capturadas não o fazem.
Mudança causal de D5 exige recalcular a resolução; mudança isolada de
dependência D6B exige refazer o plano correspondente. D5 já captura a
dependência completa de manpower dos participantes via D3: uma mudança em
qualquer coorte participante pode tornar D5/D6B2 stale, mesmo que aquela
coorte não seja exposta. D6B2 não enfraquece essa validação por atuar somente
em coortes `Available`. Mudanças em contingents, sources, Battles ou Persons
não relacionados não invalidam o plano; alteração
de capacidade de source, por si só, também não o invalida se a proposta de
morte D6B1 continuar current. A policy e cada proposta de source são
revalidadas antes de o plano ser considerado current.

D6B2 é uma operação explícita e não roda automaticamente em `AdvanceDay`.

Todas as mutações de World Truth exigidas pelo outcome aceito — incluindo a
aplicação das transições de source propostas, transições do roster/coortes,
outcome persistente e lifecycle `Active → Resolved` — formam em D7 uma única
transação lógica: ou todas se aplicam ou nenhuma. D7 revalida ou reconstrói a
resolução, o plano completo, as dependências de source e custody e a lifecycle
da Battle.
Save e observers não podem ver estado intermediário. A aplicação repetida não
duplica consequências, mudanças de posição, transições de lifecycle ou
registros derivados.
Não existe `Resolved` com consequências diretas implicitamente pendentes para
aplicação posterior, nem obrigações duráveis de consequência sem um workflow
real que as consuma. Um workflow persistente em múltiplas etapas exigiria uma
decisão e um modelo explícitos.

No primeiro modelo síncrono, o lifecycle permanece `Pending → Active →
Resolved`; não se acrescentam estados duráveis intermediários de resolução ou
aplicação. `PersistentBattleRecord` é o owner preferido do outcome singular,
com o invariável de que `Pending/Active` não possui outcome terminal e
`Resolved` possui exatamente um outcome válido e seu `ResolvedAbsoluteDay`.
Esse dia é o dia lógico vigente no boundary de aceitação/commit. Se o dia mudar
depois de preview ou computação, o contexto antigo fica stale e a resolução é
refeita, sem backdating silencioso. Um dia efetivo diferente só pode existir
como input factual explícito.

### Eventos, history e determinismo numérico

O evento `BattleResolved` de D7 v1 é uma projeção downstream do outcome aceito
e só é tentado após o commit autoritativo. Seu conteúdo semântico mínimo pode
identificar BattleId, dia, Victory/Draw, vencedor quando houver, sides e
participantes por identidade estável, e provenance/fingerprint aceita. Não
carrega snapshot do mundo, scores brutos ou IDs de Conflict lower-level. Se o
registro de evento/history falhar depois do commit, World Truth permanece
commitada e o resultado de D7 informa o aviso pós-commit; não há rollback nem
republicação automática em uma chamada idempotente. Entrega confiável futura
exigiria mecanismo explícito de retry/outbox. Event/history não são a
autoridade primária nem tornam o sistema event-sourced.

O uso atual de `float` permanece aceitável para computação bruta efêmera no
perfil numérico compatível. D7 v1 pode aceitar e persistir um outcome quando a
policy de resolução identifica um perfil explicitamente suportado pelo host.
Esse perfil compõe a compatibilidade autoritativa da resolução: além dos
mesmos inputs autoritativos e versão compatível, as execuções precisam usar o
mesmo perfil numérico suportado. O perfil atual
`unity-float32-current-host:v1` descreve a composição Unity que o declara; não
certifica automaticamente outra versão/runtime Unity, headless ou
`Simulation.Core`. Outro host só pode declarar compatibilidade com a mesma
identidade após estabelecer essa compatibilidade; caso contrário, deve falhar
fechado para a resolução autoritativa sob esse perfil.

```text
SUPPORTED NUMERIC PROFILE
!= PROVEN ARBITRARY CROSS-HOST EQUIVALENCE
```

Não se afirma equivalência arbitrária cross-host ou cross-profile. Perfis
diferentes não satisfazem o mesmo contrato de compatibilidade para resolução;
replay ou recomputação entre perfis permanece não suportado/não comprovado.
Nenhuma estratégia numérica universal — quantização, aritmética definida,
fixed-point/integer ou outra — é escolhida por esta regra.

```text
ORDERED RETREAT != ROUT != SURRENDER
BATTLE STARTED != FIGHT UNTIL ANNIHILATION
```

Victory em batalha não transfere automaticamente ownership, sovereignty,
jurisdiction, administration ou allegiance. Military control é uma verdade
factual sobre a capacidade militar efetiva de controlar uma localização
territorial relevante. Pode mudar por batalha, surrender, defection,
withdrawal, colapso local, abertura de portões ou outros fatos, mas presença
militar não equivale a controle.

Military control deve se aplicar à Location ou territorial place que possua
semântica reconhecida no mundo. Não se fixa antecipadamente uma hierarquia
city/region/kingdom nem uma fórmula universal de porcentagem. Uma região maior
pode derivar seu estado das localizações relevantes quando isso fizer sentido;
quando a granularidade interna for importante, ela deve usar sublocations
reais, como uma cidadela ou porto. Os estados podem incluir controlled,
disputed e ausência de controle militar relevante ou estável.

Occupation é controle militar sustentado de uma localização por uma força ou
ator que não implica transferência automática da soberania ou jurisdição
anterior. Pode criar necessidades e decisões de garrison, requisition,
administration, restrictions, resistance ou tribute/tax attempts, mas
`Occupation = true` não produz todas essas consequências sem a regra, fato ou
decisão correspondente.

War goals pertencem aos participantes, e uma guerra pode ter múltiplos goals
por participante em vez de um único objetivo global. É necessário distinguir:

```text
ACTUAL WAR GOAL != PUBLIC / DECLARED WAR GOAL
                   != BELIEVED ENEMY WAR GOAL
```

Goals podem mudar por decisões, e atingir um goal não termina
automaticamente a guerra. WarGoal não é uma quest engine.

War é mais que uma coleção de batalhas: é um contexto persistente de violência
coletiva sustentada. Pode gerar mobilization, funding, production, security,
logistics, pressure política e social reactions, mas a existência de guerra
não aumenta impostos, converte profissões, nem determina apoio da população
automaticamente. Essas consequências dependem de regras, instituições,
decisões e fatos adequados.

A pressão para encerrar uma guerra deve emergir de casualties, esgotamento de
manpower, shortage, forças sem pagamento, debt, taxes, produção perdida,
occupation, morale, oposição política, social reactions, aliados perdidos,
goals alcançados e expectativa de sucesso. Não criar um `WarExhaustion` como
verdade causal mágica; os atores decidem continuar ou encerrar usando seu
conhecimento.

```text
CEASEFIRE != SURRENDER / CAPITULATION != PEACE != WAR TERMINATION
```

Uma guerra pode terminar sem tratado formal. Participantes podem sair
separadamente ou mudar de lado quando os domínios apropriados permitirem, e a
saída de um participante não termina necessariamente a guerra. Evitar
`War.Winner` como verdade primária universal; o resultado pode ser descrito
por goals alcançados ou falhos, mudanças de controle, casualties, surrender,
saídas e consequências políticas. O Conflict mais amplo pode sobreviver ao
fim da War.

Campaign permanece **DEFERRED**. Não deve ser criada apenas porque uma guerra
pode conter várias operações; ela será reavaliada quando existir um consumidor
real que precise agrupar operações sob um objetivo operacional comum.

Tratados e compromissos diplomáticos continuam possuindo semântica própria:

- aliança;
- tributo;
- embargo;
- cessar-fogo;
- tratado;
- reconhecimento.

Eles compõem organizações, relações, conhecimento e obrigações. Diplomacia
não deve ser reduzida a um único valor de amizade entre Estados.

---

# Parte XVI — Espaço, exploração e conhecimento espacial

## 69. Fundação física regional: HexGrid, Locations e topologia local

**DECIDIDO / DIREÇÃO**

O mundo físico utilizará um HexGrid como substrato espacial regional comum.
Um Hex é uma área física relativamente grossa do mundo, não uma coordenada de
alta resolução. A arquitetura não deve criar uma recursão:

```text
World → Hex → smaller Hex → smaller Hex → coordinates
```

A estrutura conceitual é:

```text
World
  → Hex
      → Location
          → LocalTopology / SubLocation
```

SIMULATION LOCATION != RENDERING COORDINATE. Coordenadas, meshes e outras
representações do host podem existir para apresentação ou interação local, mas
não definem a posição autoritativa do mundo.

O primeiro consumidor civil dessa fundação usa um pequeno mundo finito
criado manualmente para validar verdade espacial, planejamento e viagem. Esse
recorte não limita a arquitetura a uma região: toda célula que integra o mundo
inicial existe factualmente antes da primeira fronteira simulada, mesmo sem
observador, cena carregada ou representação visual. Criação posterior de espaço
segue a autoridade normal de mutação do mundo, não a sua descoberta.

### Hex como área regional

Um Hex pode conter zero, uma ou várias Locations, além de terrain, features
físicas, barriers, crossings e outras informações regionais quando houver um
consumidor real.

Hexes existentes possuem identidade semântica estável e coordenadas semânticas
suficientes para derivar sua adjacência geométrica. Só células existentes
participam da geografia finita. Coordenada semântica não é `HexId` nem
coordenada de rendering; sua representação técnica permanece aberta. A
geometria regional é hexagonal, sem exigir suporte a grid quadrado.

```text
Hex 421
├── City A
├── Old Well
└── Stone Bridge
```

Isso não significa que Old Well esteja dentro de City A. Nem o fato de duas
entidades ocuparem o mesmo Hex cria uma relação hierárquica ou uma conexão de
traversal:

```text
CONTAINMENT != SAME HEX != CONNECTIVITY
```

Adjacência geométrica também não garante passagem:

```text
HEX ADJACENCY != TRAVERSABILITY
```

Hexes vizinhos podem estar separados por rio, canyon, parede, face de montanha,
mar, barreira mágica ou passagem colapsada.

Terrain é contexto físico regional. Seus tipos podem ser definidos por
conteúdo e não precisam formar, desde já, um enum universal fechado. Terrain
não possui um Hex.TravelTime absoluto; o custo de traversal depende de
condições do mundo, terrain, perfil de movimento, rota ou crossing e outros
fatos relevantes.

Existência do Hex, terrain aplicado, estruturas instanciadas e suas condições
atuais podem ser fatos do mundo. Tempo, esforço, custo de recursos, risco,
perigo e atratividade de uma rota são avaliações contextuais desses fatos para
um viajante e momento, não verdades escalares permanentes do Hex. Perfis
diferentes podem avaliar o mesmo caminho de modo diferente.

### Location é âncora espacial, não entidade universal

Location é uma identidade espacial neutra e estável. Não é WorldEntity,
classe-base de todo objeto do mundo ou substituto de City, Fortress, Mine,
ExplorableSite e outros conceitos de domínio.

Entidades de domínio permanecem domain-owned e referenciam uma âncora:

```text
City          → LocationId
Fortress      → LocationId
Mine          → LocationId
ExplorableSite → LocationId
```

No primeiro slice físico, cada Location possui um AnchorHexId único:

```text
Location → AnchorHexId
```

Isso não torna impossível um footprint que futuramente cubra vários Hexes. Essa
complexidade só deve ser introduzida quando um consumidor real exigir essa
semântica. Não faz parte do primeiro foundation slice.

Objetos móveis não são Locations:

```text
Person | Caravan | ArmedForce | Ship | TravelParty | Expedition
    → current spatial position/reference

MOVING ENTITY != LOCATION
```

Entidades de domínio como City e ExplorableSite permanecem distintas de suas
âncoras `LocationId`. Compartilhar um Hex não dá acesso automático a uma
Location, a seus pontos de entrada nem a outra POI no mesmo Hex. Essas
transições e sua conectividade precisam de semântica espacial explícita.

### LocalTopology e SubLocation

Interiores e estruturas locais não precisam repetir o HexGrid regional:

```text
Hex 421
└── City A
     └── LocalTopology
          ├── Market Square
          ├── Palace
          ├── Barracks
          └── Harbor
```

LocalTopology representa relações locais significativas, como containment,
conexões percorríveis, entry points, publicação e conhecimento topológico. Isso
é outro nível de abstração, não zoom infinito do grid.

O LocalTopologyStore atual é uma fundação reutilizável/adaptável. Futuramente
deve aceitar um owner espacial neutro quando necessário, em vez de permanecer
semanticamente limitado a City e ExplorableSite.

### SpatialReference

A arquitetura deve suportar uma referência física semanticamente tipada que
possa apontar para:

- Hex;
- Location;
- SubLocation;
- Crossing espacialmente identificável.

O formato técnico exato permanece para o futuro foundation slice. Não são
autoridade espacial:

- strings opacas sem validação;
- nomes textuais de cidades;
- Unity RuntimeIds;
- GameObjects;
- coordenadas de rendering.

Uma referência mais específica deve ser resolvível para sua posição regional:

```text
Market Square → City A → Hex 421
```

Uma Crossing também pode ser uma referência válida sem se tornar
automaticamente uma Location genérica.

### Traversal network

O grid físico e a rede de traversal são camadas diferentes:

```text
PHYSICAL GRID != TRAVERSAL NETWORK
```

O grid responde principalmente por posição regional, vizinhança geométrica e
contexto físico. A traversal network representa relações de movimento
semanticamente relevantes, como road, trail, mountain pass, tunnel, ferry
route, gate, bridge, portal, sea lane e causeway.

O mundo não deve virar uma railway. Nem toda passagem possível precisa de uma
Connection explícita. Quando as condições físicas permitem, wilderness
traversal pode ser derivado da geografia e do terrain:

```text
grid topology + physical conditions + explicit traversal structures
→ possibilidades de passagem
```

Adjacência geométrica não é passagem física; passagem física não é capacidade
do ator de usá-la; capacidade não é permissão legal/social; e nenhuma delas é
automaticamente a passagem percebida pelo ator. Road e bridge não criam
adjacência. Uma estrada fechada não elimina necessariamente outra passagem
física possível, e uma ponte destruída não elimina os Hexes vizinhos. Evitar
um `Hex.Blocked` universal. A política de permissão legal, política ou militar
continua distinta e não precisa ser resolvida pelo primeiro consumidor civil.

### Barrier e Crossing

Barrier representa um obstáculo ou restrição física relevante para traversal.
Pode ser um rio, canyon, parede, cadeia montanhosa, ravina, mar ou campo
mágico. Uma Barrier não precisa estar limitada a uma única boundary entre dois
Hexes; um rio pode atravessar muitos Hexes e uma parede pode cobrir vários
segmentos.

Crossing representa uma forma ou local concreto de atravessar ou superar uma
Barrier:

```text
River       → Bridge | Ford | Ferry
Mountain    → Pass | Tunnel
Wall        → Gate
```

Bridge não é uma primitive arquitetural especial. É um tipo ou definição de
conteúdo de Crossing:

```text
BARRIER → CROSSING → TRAVERSAL POSSIBILITY / CONNECTION
```

Uma Crossing pode possuir identidade estável, posição física, relação com a
Barrier, estado factual atual e restrições de traversal. Se for destruída,
Hexes e Barrier permanecem; apenas aquela Crossing se torna inutilizável.
Outras Crossings podem continuar funcionando.

Condições de estrada, obstruções e disponibilidade de passage/crossing que
possam mudar são estado factual de runtime. O primeiro consumidor civil deve
permitir ao menos uma mudança factual de passage/crossing durante a história
simulada para que a revalidação da execução tenha significado. Essa mudança
usa a autoridade normal de mutação; não é uma edição silenciosa de asset.

A traversal authority futura combina topologia derivável do grid com estado
autoritativo persistente quando a mudança factual for relevante. Não deve
persistir cada edge geométrica simples sem necessidade, mas também não deve
derivar tudo a ponto de perder estados como estrada bloqueada ou ponte
destruída.

### Travel e movimento

A verdade espacial futura não deve ser primariamente:

```text
City A → City B → TravelDays = 3
```

Deve ser:

```text
Origin
→ chosen path / route
→ traversed Hexes / connections / crossings
→ Destination
```

Travel duration é resultado da semântica de traversal:

```text
DISTANCE != TRAVEL TIME
SHORTEST PATH != FASTEST PATH != SAFEST PATH
```

Uma rota mais longa por plains e road pode ser mais rápida ou segura que uma
rota curta por montanhas.

O planejamento de rota considera posição/origem, destino, conhecimento
espacial do ator, perfil/capacidades, parâmetros efetivos pertinentes e
critério da decisão. Pode produzir alternativas, plano escolhido, estimativas
de duração/custo e assumptions/proveniência. Seleção e desempate que afetam o
resultado são determinísticos; query e preview não mutam nem consomem RNG
autoritativo. No primeiro slice civil, ao menos duas alternativas conhecidas
permitem exercitar a escolha e o efeito de conhecimento desatualizado; isso
não exige duas alternativas para toda viagem futura. O algoritmo de busca e
uma pontuação universal de rota permanecem abertos.

O plano aceito registra a intenção e suas hipóteses, não congela a realidade
física. A sequência causal é:

```text
Knowledge → route decision/plan → traversal attempt → execution context
→ factual validation → progress/outcome → observation/event/history as appropriate
```

Na fronteira causal de cada movimento, a execução revalida os requisitos
factuais do próximo segmento e calcula seu contexto/custo efetivo. Se uma
ponte caiu ou outra condição tornou o plano inviável, a execução pode parar ou
falhar na posição factual já alcançada, sem desfazer progresso válido nem
inventar silenciosamente outra rota. Replanning é nova decisão. Uma falha não
transmite automaticamente ao viajante a causa oculta: somente o que ele pode
perceber atualiza seu Knowledge. Estimativa de rota não é verdade de execução
nem custo contextual necessariamente realizado.

Um viajante em trânsito mantém posição factual: pode estar em Hex, âncora/local ou
num segmento de traversal, com direção e progresso determinístico suficientes
para retomar a viagem. Não precisa de microposição contínua nem de coordenadas
de rendering; a base temporal diária pode continuar. Uma travessia de vários
dias não torna o viajante espacialmente inexistente. Esse estado deve
permanecer autoritativo apesar de Dormant, descarregamento de representação e
futura persistência, sem depender exclusivamente de uma instância
`NpcRuntime`. Também permite que futuros consumidores de encontro ou
interceptação saibam onde ocorreu o deslocamento, sem definir encounters agora.

O primeiro viajante suportado é um indivíduo civil com `PersonId` persistente.
`NpcRuntime` pode participar da AI ou da execução rica, mas não ser a única
autoridade persistente de posição ou compromisso de viagem. Essa escolha de
slice não restringe as primitivas de passagem a Persons nem exige uma
abstração universal de Group/MovementSubject antecipadamente.

A posição factual pode pertencer à autoridade espacial vinculada a `PersonId`.
Um compromisso individual de viagem persistente integra estado individual
rico, carregado ou descarregado; a ausência de `NpcRuntime` carregado não faz
desse viajante uma `Person-only`.

O primeiro slice exercita viagem por múltiplos segmentos, mudança factual de
passagem, rejeição de hipótese obsoleta, observação apropriada, parada ou nova
decisão de rota e chegada pelo mesmo modelo autoritativo.

Perfis de movimento como Person, Caravan, Scout, Army, Mounted Group, Ship e
Flying Creature podem interagir de formas diferentes com o mesmo terrain.
O perfil suficiente para o primeiro viajante civil pode existir sem fixar uma
hierarquia universal de perfis. Capacidade, suprimento, permissões e execução
de movimento militar permanecem próprios de futuros consumidores, que podem
reutilizar a geografia, IDs, barriers, crossings e consultas factuais sem
herdar velocidade civil como verdade topológica.

### Chegar a um local != conhecer sua topologia

Arrival pode revelar ou confirmar o site macro, mas não deve automaticamente
revelar toda a topologia interna:

```text
Arrival
→ site knowledge

Begin Exploration
→ direct observation dos entry points visíveis
```

Observação deve reutilizar os sistemas de conhecimento espacial existentes, não
criar um canal paralelo.

### Knowledge espacial

Spatial authority representa a verdade física factual. Actor knowledge
representa o que aquele ator sabe, acredita ou recebeu de terceiros:

```text
World Truth:
Stone Bridge destroyed on day 128.

Traveler Knowledge:
Stone Bridge believed passable.
Observed day 120.
```

Route planning usa Knowledge. Traversal execution revalida World Truth. O
conhecimento não deve revelar automaticamente Hexes, Connections, Barriers,
Crossings ou seus estados atuais.

Observações espaciais podem ser incompletas, velhas ou falsas, inclusive sobre
destino, segmento, estrada, crossing e condição de passagem. Origem e tempo
da observação importam quando influenciam confiança ou decisão. Uma rota
lembrada pode sobreviver como plano/compromisso, mas o planejador não deve
filtrar cada passagem acreditada com a verdade oculta antes de considerá-la:

```text
NO KNOWN ROUTE != PHYSICALLY IMPOSSIBLE ROUTE
PLAN STALE IN TRUTH != ACTOR KNOWS WHY
```

Por exemplo, o viajante pode escolher uma ponte que acredita utilizável. Ao
alcançá-la, a execução verifica sua condição factual; se ela caiu, o ator
permanece onde chegou e só aprende aquilo que consegue observar. Outra rota,
se conhecida, exige nova decisão.

### Battle location e aftermath

Uma Battle não exige previamente uma entidade Battlefield. A localização do
episódio pode ser um Hex, Location, SubLocation ou Crossing:

```text
Pending Battle → localização ainda opcional
Active Battle  → localização física factual válida obrigatória
```

WORLD TRUTH: Battle exists at Hex X não concede esse conhecimento a todos os
atores.

### Compatibilidade física entre Battle e participantes

A presença física deve usar inicialmente uma regra conservadora e consciente
da containment. Uma posição mais específica pode provar presença em uma área
mais ampla, mas uma posição mais ampla não prova presença em uma área mais
específica:

```text
Battle at Hex H
  Force at Hex H                         → compatível
  Force at Location anchored in H        → compatível
  Force at SubLocation anchored in H     → compatível

Battle at Location L
  Force at Location L                    → compatível
  Force at SubLocation contained in L    → compatível
  Force only at Anchor Hex               → insuficiente

Battle at SubLocation S
  Force at exact SubLocation S           → compatível
  Force only at parent Location          → insuficiente
  Force only at Hex                      → insuficiente
```

Isso preserva:

```text
MORE-SPECIFIC PRESENCE MAY PROVE BROADER PRESENCE
BROAD PRESENCE DOES NOT PROVE MORE-SPECIFIC PRESENCE
SAME PHYSICAL AREA != SAME EXACT SPATIAL REFERENCE
```

O modelo não inventa geometria local inexistente. A compatibilidade regional
de uma Location ou SubLocation deve ser resolvida pelas âncoras espaciais
existentes, sem transformar o fato de compartilhar um Hex em presença
automática dentro de uma estrutura específica.

Battle != Battle Location != Battle Aftermath / Battlefield Site.

Uma Battle em City A → Market Square pode deixar corpos, feridos, equipamento,
destroços ou danos associados à própria Market Square. Não deve criar
automaticamente Battlefield #123.

Se uma batalha em um Hex sem Location produzir uma identidade espacial
persistente relevante — por ruínas, túmulos, fenômeno sobrenatural, memória
social ou outro fato — uma nova Location/POI pode ser criada explicitamente:

```text
Hex 1837
└── Battlefield of Red Spears
```

Essa criação depende de relevância semântica real. BATTLE ENDED != BATTLEFIELD
MATERIAL STATE ENDED, e material aftermath não é a mesma coisa que memória
histórica da Battle. O aftermath pode ser saqueado, removido, enterrado,
degradado, queimado ou recuperado sem apagar a Battle histórica, knowledge,
memories ou consequências já produzidas.

Relevant Persons podem futuramente possuir restos individualizados quando a
identidade importar, mas casualties agregadas não exigem um objeto de corpse
por indivíduo. A semântica completa de corpses, scavenging, looting, burial,
salvage e aftermath permanece deferida até existir Battle Outcome e consumidor
real. Não criar ainda um LootPile ou CorpseSystem universal.

### Escala física

Não se fixa:

```text
1 Hex = exactly X km
```

Cada world/content context deverá possuir uma convenção física única e coerente
quando for necessário calcular distance, movement, speed, traversal, time,
logistics ou operational range. Não se adota, neste momento, escala regional
variável. Para um mundo que usa viagem física, a escala/convenção regional
resolvida participa do contexto efetivo autoritativo; parâmetros globais de
cálculo de viagem, quando realmente globais, pertencem à configuração efetiva.
Definições de terrain, estruturas e perfis permanecem conteúdo, enquanto
terrain aplicado e condição atual são World Truth. A unidade e o valor exatos
da escala não são constantes universais. Distância em Hexes, distância física
e tempo de viagem são grandezas diferentes: um Hex não equivale a um dia de
viagem. Rendering scale não participa dessa autoridade.

### Identidade, autoridade e determinismo

A futura spatial foundation deverá possuir identidades semânticas estáveis para
Hex, Location, SubLocation, Connection, Barrier e Crossing quando esses
conceitos tiverem identidade persistente. RuntimeIdAllocator, ordem de
descoberta Unity, ordem incidental de assets e coordenadas de rendering não são
identidade conceitual final.

`HexId` e `LocationId` são identidades de instância, distintas de coordenada,
nome exibido e `DefinitionId`. Estruturas como uma ponte, road ou crossing
individual também precisam de identidade semântica própria quando sua
continuidade, estado ou história importarem. A forma de atribuir IDs em futura
geração procedural permanece aberta. A origem manual, gerada, modded ou criada
por mutação validada não muda a ontologia nem a autoridade espacial.

Ordenações relevantes, como vizinhos de Hex, alternativas de passagem,
Locations, Connections, Crossings, observações, candidatos de rota, progresso
e desempates de path, devem ser semânticas ou determinísticas. O
algoritmo de pathfinding continua deferido, mas deterministic tie-breaking não
é opcional para pathfinding autoritativo.

```text
CONTENT DEFINITION != CURRENT SPATIAL WORLD STATE
```

Uma BridgeDefinition pode descrever tipo, material e propriedades padrão,
enquanto o runtime mantém a condição factual Destroyed. Uma TerrainDefinition
descreve comportamento padrão, enquanto o estado do mundo determina qual
terrain/contexto se aplica ao Hex.

A autoridade futura deve ser distinta de rendering Unity, GameObjects, caches
de path, knowledge de atores e definições de conteúdo:

```text
content definitions
↓
authoritative physical spatial state
  ├── Hex / grid topology
  ├── Location anchors
  ├── Barriers
  ├── Crossings
  └── traversal state / connections
↓
LocalTopology
↓
derived indexes / path caches
↓
actor Knowledge
```

O nome técnico dessa authority não precisa ser congelado agora.

### SpatialNetworkRuntime atual e migração

SpatialNetworkRuntime atual é ADAPT / TRANSITIONAL. Ele pode continuar servindo
consumidores/cenários explicitamente legados ou uma projeção de compatibilidade
enquanto o modelo baseado em SpatialLocationRuntime + SpatialRouteRuntime +
fixed TravelDays ainda for necessário. Não é a authority espacial final nem
uma segunda fonte de passagem factual, posição, progresso ou duração dentro
de uma viagem que use as novas semânticas. Nesses mundos/cenários, o novo modelo
é a authority espacial e de viagem. Cada viagem/viajante possui uma só
autoridade espacial e de execução; coexistência entre cenários não autoriza
dual authority dentro de um mesmo trip.

A migração deve ser incremental:

1. introduzir referências físicas estáveis;
2. introduzir uma authority mínima de Hex e Location;
3. mapear o modelo atual para Location/Hex e Connections transitórias;
4. preservar Travel através de adapter/facade;
5. permitir conteúdo novo usar Hexes reais sem migrar todo cenário legado;
6. adaptar owners de LocalTopology para uma âncora espacial neutra;
7. evoluir knowledge para Hex/Connection/Barrier/Crossing;
8. migrar Travel para paths compostos;
9. fazer Merchant e Expedition consumirem a nova authority;
10. só depois integrar Battle resolution, terrain e military movement.

Não fazer um big-bang rewrite.

### P7-D0/D1 — Spatial Reference / Battle Location Foundation

P7-D0 estabeleceu o menor bridge autoritativo de referências físicas, e P7-D1
fez `PersistentBattle` consumir essa autoridade sem iniciar movimento ou
resolução. O foundation inclui:

- identidade estável de Hex e Location;
- SpatialReference tipada;
- âncora Location → Hex;
- integração mínima com LocalTopology/SubLocation;
- owner explícito da verdade espacial;
- ordenação determinística;
- snapshot, canonical output, diff e invariants básicos;
- capacidade de validar a localização física de uma Battle;
- localização opcional em Battle Pending;
- localização física válida obrigatória em Battle Active.

Se necessário para representar uma referência espacial de forma correta,
Crossing pode receber o menor contrato de identidade exigido. P7-D0 não deve
implementar HexGrid completo, pathfinding, terrain simulation, traversal
network completa, weather, movement profiles, Travel rewrite, replanning,
stale crossing knowledge novo, Merchant/Expedition migration, military
movement, scouting, Battle resolution, Battle aftermath, operational groups,
renderer, procedural generation, save/load ou networking.

P7-C, D1, D2 e D3 permanecem fechados e corretos. D2 estabeleceu a posição
física tipada e opcional de `ArmedForce`; D3 estabeleceu elegibilidade física,
composição direta, metadata de comando e `BattleExecutionContext` com
fingerprints e validação stale. Esses checkpoints não implementam movement ou
Battle resolution.

P7-D4 estabeleceu o primeiro adapter determinístico de
`BattleExecutionContext` para `ConflictFoundation`, com regra explícita de
capability, aleatoriedade contextual e `BattleResolutionComputation` efêmera.
D4 termina na computação bruta: não aplica outcome, consequências, casualties,
lifecycle, events ou history; `PersistentBattle` permanece `Active`. A
computação bruta não é ainda autorização para alterar World Truth.

### P7-D5 — Authoritative Battle Resolution Policy + Outcome/Application Plan

D5 estabelece a policy autorizada de resolução pertencente ao mundo/composition
boundary, a seleção de capability, random authority, resolver settings, versão
de projeção e perfil numérico; a recomputação autoritativa a partir de
`BattleId` e estado atual; a semântica opcional de preview, fingerprint esperada
e confirmação; o contrato mínimo de `BattleOutcome` e sua provenance; e o
contrato de plano imutável, revalidação e stale handling. Uma computação
arbitrária fornecida pelo caller nunca é aplicada como verdade.

D5 não inclui casualties, alteração de `Contingent.Amount`, transição
`Active → Resolved`, mutações de mundo ou eventos/history.

### P7-D6A — Military Manpower Source / Roster / Availability Foundation

D6A estabelece a identidade e binding das fontes de manpower, alocação militar,
estado agregado do roster vivo e disponibilidade por coortes, incluindo o
tratamento explícito de contingents legados sem fonte. Essa etapa integra a
disponibilidade à elegibilidade e aos inputs causais de D3/D4/D5, além dos
snapshots, canonicalização e invariantes necessários para verificar
conservação. A implementação não infere source binding de
`ContingentOriginReference`.

D6A não define o que uma Battle causa, não inclui regra de casualty nem aplica
outcome. Também não transforma `LossFraction` em headcount nem consome RNG de
consequência.

### P7-D6B1 — Manpower Source Consequence Planning

Depois de D6A, D6B1 estabelece a fronteira que traduz um efeito de manpower
que altera uma fonte autoritativa em uma proposta imutável do domínio dono
dessa fonte. A primeira aplicação é uma source populacional baseada em
`SettlementPopulationRuntime`, vinculada por composição explícita e protegida
pelo piso factual de residentes representados. A proposta reutiliza a
autoridade de demografia agregada, mantém capacidade militar alocável distinta
de população viva e captura somente as dependências necessárias à transição.
D6B1 não decide casualties, não altera roster ou fonte e não aplica a proposta.

### P7-D6B2 — Battle Direct Consequence Planning

D6B2 parte de `BattleId`, reconstrói o D5 autorizado e combina seu outcome
semântico com participantes/coortes D3/D6A e uma policy de consequência
distinta da policy de resolução bruta. Na v1, a regra é executada uma vez por
Battle sobre todas as coortes expostas, produz partições explícitas e
determinísticas, e não introduz RNG de consequência nem fórmula de casualty de
produção. Mortes são agregadas por `ManpowerSourceId` antes de uma única
proposta D6B1 por source afetada; wound e capture não reduzem roster, e capture
exige custodian participante válido. O plano imutável preserva conservation,
rastreabilidade, dependências estreitas, revalidação e a distinção entre
consequence-complete e commit-ready; não muta World Truth. Aleatoriedade de
consequência futura exige decisão própria e autoridade contextual separada da
aleatoriedade de resolução bruta:

```text
RAW RESOLUTION RNG != CONSEQUENCE RNG
```

A cobertura D6B2 fornece o plano de consequências que D7 aplica; não é
necessária outra gate exploratória entre esses contratos, salvo se uma
implementação revelar contradição concreta. A arquitetura de D7 está definida
abaixo; sua implementação permanece posterior à validação e promoção de D6B2
e exige que P7-D7G seja implementado, validado e promovido primeiro.

### P7-D7 — Atomic Battle Outcome Application

**DECIDIDO**

D7 é uma operação explícita, síncrona e vinculada ao runtime autoritativo. Sua
entrada começa por `BattleId` e pode incluir apenas metadata de execução D3 já
permitida por D5 e uma fingerprint D6B2 esperada opcional. O chamador não
fornece outcome, plano D5/D6B2, proposta de source, pós-estado de manpower ou
policy. D7 reconstrói internamente um plano D6B2 atual a partir das authorities
do mundo. Sem policy D6B2 configurada, falha fechado; ausência de regra não é
uma consequência zero nem autoriza fallback de casualty.

Se fornecida, a fingerprint esperada do plano D6B2 é uma precondição de
preview-confirmação: deve coincidir com o plano recém-calculado antes de
qualquer mutação; mismatch rejeita sem alterar World Truth. Se omitida, D7
aplica o plano atualmente autorizado. Essa confirmação pertence à fronteira
de aplicação, não ao planner D6B2.

#### Outcome aceito, ownership e lifecycle

O outcome persistido é um valor imutável mínimo: BattleId, Victory ou Draw,
WinningBattleSideId apenas para Victory, dia lógico da resolução e provenance
aceita. A provenance identifica a policy D5, perfil numérico, versão da
projeção e fingerprint causal, além da policy D6B2, versão/schema de plano e
coverage e fingerprint do plano de consequências aceito. Não guarda
referências a policies ou regras vivas.

`PersistentBattleRecord` possui o único outcome terminal da Battle;
`PersistentBattleStore` possui a operação de domínio que grava esse outcome e
faz `Active → Resolved` em conjunto. Não há um segundo outcome store em v1.
Registro público normal não pode criar Battle resolvida; eventual hidratação
de save/load terá um caminho validado próprio.

Os invariantes são:

- `Pending` não tem dia de início nem outcome terminal;
- `Active` exige dia de início e localização física válida, e não tem outcome;
- `Resolved` preserva início/localização e tem exatamente um outcome coerente,
  com dia de resolução igual ao dia do outcome e não anterior ao início;
- o BattleId do outcome coincide com o record; vitória aponta para uma
  `BattleSideId` existente e empate não possui vencedor.

`BattleId` é a raiz de idempotência. A primeira aplicação retorna
conceitualmente `Applied`; uma chamada posterior para a mesma Battle retorna
`AlreadyResolved`, expõe o outcome persistido e não recalcula D5/D6B2, não
reaplica consequências e não reemite o evento. Em retry com fingerprint
esperada, igualdade com a fingerprint aceita mantém `AlreadyResolved`; uma
fingerprint diferente retorna mismatch explícito sem mutação. Ler um outcome
resolvido não depende das policies atuais; mudanças posteriores de policy não
reescrevem nem invalidam o fato já aceito.

#### Transições de domínio e conservação

D6B1 continua sendo a authority de planejamento de efeitos de source. D7 usa
um boundary estreito de aplicação pertencente ao domínio da source e vinculado
à mesma registration explícita usada na proposta. Para source populacional de
settlement, aplica-se a `AggregateDemographyTransition` capturada por meio da
authority de demografia agregada (`AggregateDemographySystem.TryApply`). D7
não edita população diretamente, não chama novamente o provider demográfico
nem recalcula mortes.

A aplicação do manpower pertence a um batch específico de Battle na authority
D6A e consome o pós-estado exato projetado por D6B2. Não se interpreta morte
como desmobilização nem se usa uma redistribuição que exige preservar o roster
vivo para reduzi-lo. Antes de escrever, o batch verifica para todos os
contingents afetados o pre-state/revisão atuais, o pós-estado projetado, a
preservação do source binding, a redução do roster exatamente igual às mortes
D6B2, coortes e custódia válidas, e a possibilidade de avançar revisões e
mirrors.

```text
Contingent.Amount = LivingRosterAmount
SUM(military deaths by source) = D6B1 source death proposal
post source allocation = pre source allocation - those deaths
```

Morte altera o mirror `Contingent.Amount` junto com o roster no mesmo batch;
ferimento e captura, por si só, não o alteram. Nenhum setter arbitrário de
Amount é introduzido. Source populacional deve reduzir a população factual
exatamente pelo efeito de morte proposto, sem aplicar ou omitir a mesma morte
duas vezes. Se o estado projetado de um contingent for semanticamente igual ao
atual, ele não é escrito nem recebe nova revisão. Um plano completo sem
consequências também é válido: nesse caso só o outcome/lifecycle da Battle
muda.

#### Commit lógico e falhas

D7 v1 usa uma transação estreita e específica de Battle, não um framework
universal de transações, Unit of Work, undo commands ou engine genérica de
mutações. Seu boundary tem quatro momentos: recomputar o plano autorizado;
preparar e validar todos os writes e snapshots de rollback sem mutação; aplicar
somente writes preparados; e, por fim, tentar evento/history. A preparação
inclui policy/perfil, Battle ainda `Active`, freshness D5/D6B2/D6B1,
destinos exatos de source, pós-estados de manpower e mirrors, record terminal
e todos os limites de revisão.

A ordem dos writes é: (1) batch de coortes/manpower e mirrors; (2) transições
factuais de source em ordem estável de `ManpowerSourceId`; (3) outcome e
`Active → Resolved` como último write do Battle. Assim, a alocação militar cai
antes da população de sua source, e a Battle não se torna terminal antes das
consequências diretas. Nenhum evento, history, callback, policy/provider,
código do usuário, `await`, snapshot de save ou processamento de comando pode
observar ou intercalar o estado entre o primeiro e o último write. Código de
regra/provider roda antes do commit.

Falhas esperadas — como plano stale, source ou custody inválida, perfil não
suportado e overflow — são descobertas durante a preparação, deixam
World Truth inalterada e não faultam o runtime. Rollback é obrigatório para
falha inesperada durante o commit e restaura exatamente os valores e revisões
apenas do estado autoritativo tocados pela transação: contingents/manpower,
mirrors, sources e record/revisão da Battle. Se o rollback concluir, a falha
informa que o commit foi revertido e o runtime permanece Healthy; se o rollback
falhar, o runtime fica explicitamente Faulted e a mutação autoritativa normal
deve parar conforme a fronteira suportada definida em P7-D7G. Recuperação de
crash durável entre writes pertence à futura persistência.

D7 v1 é síncrono, sem `await`, no contexto serializado de mutação autoritativa
do runtime; não implica thread-safety geral. Chamadas D7 concorrentes ou
reentrantes no mesmo `SimulationRuntime` são rejeitadas, sem introduzir
locking assíncrono amplo.

Cada contingent alterado avança sua revisão uma vez; a revisão global de
manpower avança uma vez pelo batch; ArmedForceStore avança se algum Amount
mirror realmente mudar; cada settlement afetado aplica sua transição e revisão
uma vez; e o BattleStore avança uma vez pelo commit terminal. Estado inalterado
não recebe churn de revisão. Overflow é verificado antes do commit, e
convenções existentes de revisão do domínio não são violadas por simetria
cosmética.

O resultado distingue aplicação (`Applied`), aplicação com aviso pós-commit
(`AppliedWithPostCommitWarning`), repetição idempotente (`AlreadyResolved`) e
rejeição pré-commit (`Rejected`), com causa tipada. Se o evento/history falhar
depois de World Truth estar commitada, o resultado continua sendo aplicação
com aviso, nunca rejeição transacional. Falha de rollback é mais grave e
faults o runtime.

#### Projeções, persistência e fronteiras

Após o commit, o evento/history mínimo de BattleResolved é tentado como
representação downstream, não participante da transação. A gravação usa
identidades estáveis e provenance aceita, sem scores ou breakdown de
casualties. A chamada `AlreadyResolved` não publica de novo evento ausente;
outbox/retry confiável permanece futuro.

Como o outcome terminal é World Truth, snapshots, canonical output, diff,
formatter e invariants devem expor lifecycle, dia resolvido, tipo de outcome,
vencedor e provenance aceita. O plano D6B2 permanece efêmero e fora do
`WorldStateSnapshot`. O outcome deve ser representável futuramente por IDs
estáveis, enums, dia lógico e fingerprints/version strings, sem referências a
objetos runtime; D7 não implementa save/load ou migração/hidratação. Uma Battle
resolvida deve poder ser consultada sem recomputar D4/D5/D6B2.

D7 não encerra Conflict ou War, decide vencedor/goals de War, faz paz, altera
ownership/jurisdição/controle, move forças nem cria casualties de Persons. Não
adiciona resolução automática a `AdvanceDay`. D7 não implementa movement,
retreat/rout/surrender, aftermath amplo, save/load, replay, networking ou
event sourcing.

A validação deve provar aplicação multi-source com mortes, ferimentos,
captura, source totals e mirrors exatos; Battle de consequência zero; retries
idempotentes e fingerprint incompatível; e que toda falha pré-commit preserva
o snapshot. Falhas injetadas após o batch de manpower, após source e antes do
write terminal da Battle devem restaurar exatamente estado e revisões. Também
deve provar que mudança de policy após resolução não altera o outcome aceito e
que falha de evento pós-commit retorna aviso sem rollback.

Permanecem deferidos além dessas fronteiras:

- aftermath completo, corpses, loot e scavenging;
- execução de retreat movement, semantics avançadas de rout/surrender,
  recovery loops e prisoner release/exchange;
- military control, occupation, War progression, knowledge propagation,
  appraisal e reactions;
- tactical objectives persistentes, modifiers militares causais e `GM
  ForceOutcome` para Battle;
- military movement, Hex pathfinding, terrain, crossings, múltiplas presenças
  da mesma força, `OperationalGroup` persistente, posição espacial de Person e
  military knowledge;
- save/load específico do resultado, replay, networking, product preview e
  migração ampla para `Simulation.Core`.

### P7-D7G — Runtime Authoritative Mutation Guard

**DECIDIDO**

P7-D7G estabelece a proteção de integridade runtime exigida pela falha
catastrófica de rollback de D7. É um pré-requisito de implementação de D7, não
uma extensão da transação de Battle nem um mecanismo geral de transações. O
conceito pode ser chamado `AuthoritativeMutationGuard` ou equivalente; o nome
técnico exato não é parte do contrato arquitetural.

#### Saúde do runtime e alcance da garantia

Cada composição de `SimulationRuntime` possui um estado de integridade local,
conceitualmente `Healthy` ou `Faulted`. A transição é pegajosa e unidirecional
em v1: somente uma falha catastrófica de integridade — principalmente uma
falha ao restaurar o estado após rollback — pode levar de `Healthy` a
`Faulted`; não há reset de gameplay. Rejeições normais de domínio, como estado
stale, configuração ausente, precondição inválida ou recurso insuficiente,
não faultam o runtime. Isso não introduz singleton global, semântica genérica
de transação nem gerência geral de locks.

Enquanto `Faulted`, devem rejeitar antes da primeira escrita:

- operações autoritativas pelas APIs suportadas do runtime;
- mutações pelas authorities/stores autoritativas vinculadas à composição;
- entrypoints de sistemas usados pelo processamento normal daquele runtime.

Queries, leituras e diagnostics continuam permitidos para inspeção. A garantia
delimita a **fronteira suportada de mutação autoritativa**; ela não promete
imutabilidade de memória nem congelamento de todo o grafo de objetos alcançável
por referências externas:

```text
SUPPORTED AUTHORITATIVE MUTATION BOUNDARY
!= ENTIRE REACHABLE OBJECT GRAPH
```

Objetos legados mutáveis, como `NpcRuntime`, `CityRuntime`, coleções ou
subobjetos expostos e referências Unity retidas externamente, podem permitir
chamadas diretas que não são interceptáveis sem uma refatoração ampla de
encapsulamento. Depois de `Faulted`, essas chamadas diretas constituem
**UNSUPPORTED OUT-OF-BOUND MUTATION**, não uma operação suportada do runtime.
Isso não reclassifica `NpcRuntime` ou `CityRuntime` como projeções não
autoritativas: eles podem conter estado factual ou individual rico. Apesar
desses escape hatches legados, todo processamento normal do runtime que os use
deve rejeitar enquanto Faulted. Os bypasses identificados permanecem dívida
técnica de encapsulamento; não exigem uma reescrita total para D7.

#### Vinculação e enforcement

O guard pertence à composição do mundo: cada `SimulationRuntime` tem um, e
todas as stores/authorities mutáveis vinculadas ou clonadas para aquele mundo
consultam o mesmo estado. O fault de um mundo não afeta outro. Uma authority já
vinculada a um runtime não pode ser reatribuída a outro. Stores criadas
independentemente podem continuar utilizáveis como stores standalone
`Healthy`/não vinculadas; ao serem vinculadas, passam a respeitar o guard em
todos os caminhos suportados de mutação e a vinculação é de um único runtime.

Compartilhar a mesma authority mutável entre duas composições com guards
distintos é incompatível com essa propriedade. A composição deve rejeitar o
compartilhamento, obter ownership independente por clonagem quando já houver
essa semântica, ou declarar explicitamente uma exceção legada ainda não
resolvida; nunca associar silenciosamente dois guards à mesma authority.

O enforcement precisa existir na própria autoridade de mutação das stores
autoritativas expostas pelo runtime, antes da primeira escrita. Uma checagem
apenas nos chamadores do `SimulationRuntime` não basta quando a store também é
acessível diretamente. Sistemas podem rejeitar mais cedo, mas seus entrypoints
suportados também devem verificar o guard quando alteram estado autoritativo
fora de uma store protegida. A auditoria de D7G deve seguir os caminhos reais
de escrita das authorities compostas que forem mutáveis — como Person,
ArmedForce/manpower, Conflict/War/Battle, espaço, população/demografia,
genealogia, instituições/cargos, propriedade/estate e política/conhecimento —
e dos fluxos normais de crime/justiça, viagem, economia mercantil, expedições,
conteúdo de lugar e diretivas agendadas. Verificações redundantes não são
necessárias quando todas as escritas já passam pela mesma authority protegida.

#### Tempo, restauração e integração com D7

`AdvanceDay` normal deve verificar a saúde antes de qualquer mutação diária,
inclusive antes de avançar o dia lógico. Se Faulted, a operação rejeita sem
executar sistemas diários. A fronteira pública deve comunicar essa rejeição de
forma explícita; `TryAdvanceDay` ou equivalente é uma possibilidade, não um
contrato de API. `AdvanceDays` deve rejeitar antes de começar seu primeiro dia
quando o runtime já está Faulted. Como `SimulationTime` é exposto e possui
mutação direta, sua via normal de mutação também respeita o mesmo guard quando
vinculada ao runtime; uma instância standalone e não vinculada pode permanecer
utilizável normalmente.

A restauração interna do rollback não é gameplay nem mutação autoritativa
normal. D7 deve poder tentar restaurar os valores e revisões exatos sob uma
authority privada de restauração; se a restauração tiver sucesso, o runtime
continua Healthy, e, se falhar, o runtime passa a Faulted. Caso a implementação
marque Faulted antes de tentar restaurar, a restauração ainda precisa de um
bypass interno equivalente. Esse bypass nunca é exposto como API pública.

Quando D7 vier a ser implementado, verifica o guard antes de planejar. Um
runtime já Faulted rejeita imediatamente, sem chamar D5/D6B2 ou planners de
source, consumir RNG, alocar IDs de evento ou alterar estado. Rejeições normais
de D7 mantêm o runtime Healthy. A lease contra reentrância/concorrência da
operação D7 permanece distinta do latch de integridade e não implica
thread-safety geral.

APIs de falha `Try`/out devem, quando razoável, identificar explicitamente o
runtime Faulted; APIs mutadoras booleanas retornam `false` sem mutar. Exceções
não devem ser o fluxo esperado de controle quando a forma da API permitir uma
rejeição explícita. Um `LastMutationFailure` mutável compartilhado não é a
semântica primária de falha.

#### Diagnostics, determinismo e validação de D7G

O estado `Healthy`/`Faulted` é integridade operacional, não `World Truth`.
Pode ser exposto em diagnostics de runtime/integridade, com uma categoria ou
razão estável opcional. Exceções, timestamps de parede e stack traces arbitrários
não se tornam fatos causais do mundo. Não se inclui o latch em
`WorldStateSnapshot` sem uma necessidade concreta: snapshots de estado do
mundo e diagnostics de integridade têm propósitos distintos.

Uma rejeição por Faulted ocorre antes de efeitos colaterais que alterariam o
futuro determinístico, incluindo knowledge, RNG stateful autoritativo,
allocators de IDs, sequências de registros, diretivas ou agendas. O guard não
adiciona RNG, timestamps, ordenação global ou inputs de decisão; em runtime
Healthy, o comportamento semântico permanece inalterado.

A validação de D7G deve cobrir, no mínimo: comportamento Healthy preservado;
`AdvanceDay` rejeitado antes de mudar `SimulationTime`; mutações pelas stores
expostas de Person, ArmedForce/manpower, Conflict/War/Battle, espaço e
população/source bloqueadas, além de entrypoints representativos de sistemas;
queries e diagnostics ainda disponíveis; isolamento entre dois runtimes;
rejeição normal sem fault; possibilidade de restauração interna;
impossibilidade de rebind entre runtimes; e ausência de consumo prematuro de
RNG/IDs/sequências. Também deve documentar os bypasses de referência legados
encontrados e provar que o processamento normal por eles é bloqueado.

P7-D7G não implementa D7, framework transacional genérico, thread-safety ampla,
recuperação de save/crash, conversão geral de objetos em imutáveis ou
refatoração completa de encapsulamento. A sequência da Phase 7 é:

```text
D6B2 canonical
→ D7 architecture canonical
→ D7G implementation
→ D7G validation and canonical promotion
→ D7 implementation
→ D7 validation and canonical promotion
```

Logo, a implementação de D7 deve aguardar P7-D7G canônico. A dívida de
referências legadas fora da fronteira suportada não bloqueia D7 quando as
authorities compostas, APIs suportadas do runtime e entrypoints do
processamento normal estiverem protegidos conforme este contrato.

---

## 70. Hierarchy edge != traversal edge

**DECIDIDO**

Parentesco espacial/containment e conexão percorrível são conceitos distintos.

Um lugar ser filho de outro na hierarquia não significa existir passagem entre ambos.

---

## 71. Não anunciar capability falsa

**DECIDIDO**

Uma action/candidate só deve existir publicamente se houver transição de domínio real que a execute.

Exemplo histórico:

```text
Secure → Explore
```

como fake mapping é proibido.

Se `Secure` não possuir semântica real, fica deferred/hidden até existir.

Princípio geral:

> Feature não executável não deve ser anunciada apenas para completar enum/UI.

---

## 72. Falha de execução pode bloquear spam sem impedir retry futuro

**DECIDIDO**

Caches de falha usados para autonomia devem impedir repetição inútil no mesmo período apropriado, mas não tornar uma falha eterna.

Exemplo histórico: suppression day-scoped.

```text
falhou hoje
→ não tenta 20 vezes no mesmo dia

amanhã / truth mudou
→ pode tentar novamente
```

Histórico da falha anterior permanece.

---

# Parte XVII — Eventos, história, memória e diagnóstico

## 73. Event operacional, History e Trace são camadas diferentes

**DECIDIDO**

| Camada | Função | Retenção |
|---|---|---|
| Domain Event | anunciar mudança concluída | curta/consumível |
| Historical Record | preservar acontecimento relevante | longa |
| Diagnostic Trace | explicar cálculo/execução | limitada/selecionável |

Nem toda mutação precisa virar event sourcing.

---

## 74. Quando usar chamada direta vs evento

**DECIDIDO / DIREÇÃO**

Operações que precisam completar juntas usam chamada/transação direta.

Exemplo:

```text
debitar comprador
retirar mercadoria
creditar vendedor
entregar item
```

Consequências independentes podem ser processadas depois de uma mudança concluída.

Exemplo:

```text
morte factual confirmada
→ estate pode abrir depois
→ instituição pode reconhecer vacancy depois
→ parentes podem reagir depois
```

Mas morto não pode agir novamente no mesmo dia; isso pertence à transição principal.

Evitar callbacks recursivos cuja ordem de inscrição decide o mundo.

---

## 75. O que merece história permanente

**DECIDIDO / DIREÇÃO**

Preservar especialmente acontecimentos que:

- mudam identidade;
- mudam autoridade;
- mudam titularidade;
- criam/encerram compromissos importantes;
- têm impacto coletivo grande;
- originam memórias duradouras;
- explicam estado persistente.

Não guardar cada passeio ou cada venda comum para sempre.

Muitos eventos econômicos podem virar séries/estatísticas, mantendo individualmente falências, acordos excepcionais etc.

---

## 76. Explicabilidade

**DECIDIDO**

Resultados importantes devem poder apontar, quando possível, para:

- causas conhecidas;
- decisão responsável;
- informações utilizadas;
- fatores/contribuições relevantes.

Não fingir causalidade perfeita onde ela é coletiva/complexa.

História causal aproximada não exige replay/event sourcing total.

---

# Parte XVIII — World State Snapshot / Diagnostics

## 77. Snapshot é diagnóstico, não Truth primária

**DECIDIDO**

`WorldStateSnapshot` é representação imutável de estado para:

- diagnóstico;
- diff;
- regressão;
- canonical output;
- digest estável.

Ele não é a fonte de verdade do domínio.

---

## 78. Snapshot é value-based e imutável

**DECIDIDO**

Depois de construído, mudar o mundo não altera o snapshot.

Capturar:

- IDs;
- enums;
- números;
- booleans;
- DefinitionIds;
- relações estruturadas.

Evitar como semântica:

- `ToString()`;
- `GetHashCode()`;
- Unity InstanceID;
- display prose;
- localized text.

Preferir não guardar `UnityEngine.Object`/runtime references na representação central.

---

## 79. Snapshot deve ser determinístico

**DECIDIDO**

Mesmo World Truth deve gerar a mesma representação.

Ordenar coleções por IDs semânticos. Não depender de:

- Dictionary enumeration;
- HashSet order;
- instance order;
- AssetDatabase order;
- reference hash code.

Build/compare/write/digest não devem:

- mutar domínio;
- consumir RNG;
- gravar event/decision;
- alocar RuntimeId por efeito colateral.

A representação canônica do snapshot deve permanecer estável para o mesmo
estado semântico. Isso não transforma todo log, trace ou detalhe técnico de
diagnóstico em resultado autoritativo nem exige igualdade bit-a-bit desses
artefatos.

Digest é ferramenta de diagnóstico/comparação, não identidade ou segurança criptográfica do mundo.

---

## 80. Snapshot deve ser modular

**DECIDIDO / DIREÇÃO**

O builder deve receber input explícito/contexto pequeno e tolerar módulos opcionais ausentes.

Sections específicas são preferíveis a framework genérico de entidades.

Exemplos:

- Metadata;
- NPCs;
- Cities;
- Spatial;
- Sites;
- Expeditions;
- PlaceContent;
- NotableItems;
- LocalTopologies;
- Persons;
- Genealogy;
- Institutions;
- Property;
- Politics.

DecisionStore/EventStore/history não são necessariamente parte do snapshot de World Truth.

---

# Parte XIX — GM, World Commands, API e IA

## 81. GM continua soberano, mas não corrompe invariantes

**DECIDIDO**

O GM pode reduzir autonomia a quase zero ou declarar acontecimentos diretamente.

Mas:

```text
GM authority may bypass plausibility/decision causality
GM authority must NOT break structural invariants
```

Exemplo: GM pode declarar que Arthur virou rei, mas o sistema deve atualizar incumbency/tenure/history de forma estruturalmente coerente, não setar um campo arbitrário.

### Controle de ator e divindade factual

**DECIDIDO**

Quando uma pessoa controla um ator, ela substitui a escolha autônoma desse
ator. Recebe somente a informação e a percepção disponíveis a ele; a escolha
humana não concede onisciência nem autoridade sobre o outcome. A execução
continua sujeita às capacidades, precondições e consequências do domínio.
Visão omnisciente pertence a um modo Observer/GM explícito.

Uma divindade factual dentro do mundo é um ator de `World Truth`, com poderes
e limites causais definidos pelo domínio. Pode usar identidade `Person` quando
for um indivíduo persistente. Não herda a autoridade externa do GM. Religião,
crença, culto e datas sagradas podem existir independentemente da existência
factual da divindade; detalhes desses domínios permanecem futuros.

```text
PLAYER DECISION AUTHORITY != OMNISCIENT INFORMATION AUTHORITY
IN-WORLD DIVINITY != GM / EXTERNAL AUTHORITY
```

---

## 82. Três níveis conceituais de intervenção do GM

**DECIDIDO**

### Level 1 — Suggest / Request normal

O GM pede/sugere algo e o domínio decide se a operação normal é possível.

```text
GM: “Arthur tenta reivindicar o trono.”
→ domínio avalia regras
→ aceita/rejeita
```

### Level 2 — Assert Fact

O GM afirma que um fato ocorreu/existe.

```text
GM: “O rei morreu.”
→ morte vira World Truth
→ simulação calcula consequências restantes
```

O fato não implica automaticamente que todos saibam, que cargo esteja reconhecidamente vago ou que sucessor assuma.

### Level 3 — Assert Fact + Authored Consequences

O GM afirma fato e parte do resultado/consequências.

```text
GM: “O rei morreu e o Conselho reconheceu Arthur.”
```

ou, quando a operação suporta outcome explícito:

```text
GM: “Este confronto aconteceu e o lado B venceu.”
```

O restante das consequências segue normalmente.

---

## 83. Relação com AuthorityMode técnico

**DECIDIDO / COMPATIBILIDADE**

A implementação histórica possui quatro modos técnicos úteis:

- `Suggest` — preview/proposal não mutante;
- `Request` — pedir ao domínio uma operação normal;
- `Declare` — autoridade externa afirma fato;
- `ForceOutcome` — outcome explícito somente onde a operação suporta resolução forçável.

Eles podem coexistir com os **três níveis conceituais do GM**:

```text
Preview-only                → Suggest técnico
GM nível 1 (tenta/pede)     → Request
GM nível 2 (afirma fato)    → Declare
GM nível 3 (fato+resultado) → Declare + comandos de consequência
                              ou ForceOutcome em operações suportadas
```

`ForceOutcome` não é “super Declare” universal.

---

## 84. Request, Declare e ForceOutcome não são equivalentes

**DECIDIDO**

- Request não deve teleportar/reescrever verdade quando o payload representa declaração direta.
- Declare pode ultrapassar plausibilidade normal, mas não invariantes.
- ForceOutcome só existe para operações com outcome resolvível/constraint suportada.
- forced fields em modo não autorizado devem ser rejeitados.
- Preview valida authority compatibility antes de Execute.
- Execute revalida mesmo se caller ignorou Preview.
- comando rejeitado pode gerar audit record estruturado.

---

## 85. API/clients não mutam domínio diretamente

**DECIDIDO**

Fluxo:

```text
External client / Unity / Foundry / CLI
→ External DTO / structured intent
→ WorldCommand
→ Preview / validation
→ Apply
→ Domain
→ World Truth
```

Não expor chamadas arbitrárias como `npc.TryApplyDeath()` para integrações externas.

Comandos de GM e outros inputs externos fazem parte da entrada determinística.
Sua causalidade deve ser definida pela ordem e pela fronteira lógica de
aplicação, pelo payload e pela autoridade do comando, não exclusivamente pelo
instante real em que chegaram ao host. Um comando rejeitado pode produzir um
registro de auditoria, mas esse registro não deve alterar `World Truth` apenas
por existir.

Preview, query e diagnostics permanecem não mutantes e não consomem
aleatoriedade autoritativa.

---

## 86. IA/NLP é tradutor, não autor automático do mundo

**DECIDIDO**

A IA de linguagem natural não deve gerar autonomamente história, ações ou consequências como fonte de autoridade.

Seu papel primário é:

```text
natural language
→ structured WorldCommand proposal
→ validation
→ domain execution according to authority
```

Sugestões criativas futuras podem existir separadamente, claramente como sugestões, não como World Truth.

---

# Parte XX — Continuidade institucional e sucessão

## 87. Factual Death != Vacancy Recognition

**DECIDIDO / IMPLEMENTADO**

Uma pessoa pode estar factualmente morta e ainda ser institucionalmente reconhecida como ocupante até que a instituição processe/reconheça a mudança.

Isso é aplicação direta de:

```text
WORLD TRUTH != INSTITUTIONAL RECOGNITION
```

---

## 88. Succession candidate != political winner

**DECIDIDO**

Fase factual/estrutural produz candidatos elegíveis.

Política escolhe/apóia/reconhece entre eles.

Execução do domínio revalida condições na hora de assumir.

```text
factual eligibility
→ claims
→ knowledge
→ interpretation
→ recognition/support/legitimacy
→ political decision
→ selected PersonId
→ domain execution
→ revalidation
→ office/property mutation
```

---

# Parte XXI — Arquitetura de futuras sociedades e Estados

## 89. Membership != Citizenship != Allegiance

**DIREÇÃO**

Uma pessoa pode:

- residir em uma cidade;
- ser cidadã/súdita de um polity;
- pertencer a uma guilda;
- integrar uma facção;
- apoiar politicamente outro grupo.

Esses vínculos não devem virar um único campo `Group`. Membership também não é
automaticamente office holding, allegiance, support ou authority; cada relação
deve preservar sua semântica própria.

---

## 90. Polity/Realm futuro

**DEFERIDO**

Um reino/Estado provavelmente será entidade estrutural diferente de uma
Institution isolada e não deve ser assumido como uma Organization genérica.

Pode agregar:

- jurisdição;
- território;
- população;
- instituições;
- tesouro;
- forças;
- relações diplomáticas.

Também pode exigir semântica própria para cidadania, allegiance, controle e
reconhecimento. Uma polity poderá conter ou coordenar Organizations,
Institutions e Factions sem que essa relação implique herança ou identidade
compartilhada.

Não implementar até existir consumer real. Quando chegar, revisar
explicitamente como se relaciona com Organization, Institution e Faction.

---

# Parte XXII — Persistence e plataforma futura

## 91. Persistência por identidade semântica

**DECIDIDO**

Save/load real precisa preservar:

- IDs;
- dia e calendário efetivo;
- valores efetivos de policy e parameter, incluindo compatibilidade, versão ou
  revisão quando semanticamente necessários;
- conteúdo e definições compatíveis necessários para interpretar o estado;
- estado RNG quando necessário;
- compromissos/planos;
- relações por ID;
- estado de stores;
- versão de schema.

Referências diretas em memória podem existir, mas persistência deve poder resolver por ID.

Índices/caches devem ser reconstruíveis.

Uma referência ao nome do preset, asset ou source configuration não é, por si
só, suficiente para reconstruir o contexto efetivo: a fonte pode mudar sem
que o save mude. Os valores efetivos relevantes, o calendário efetivo e a
compatibilidade do conteúdo precisam ser preservados ou reconstruídos de forma
inequívoca.

Quando uma relação individual de membership ou participação precisar persistir,
ela deve referenciar a identidade persistente apropriada, normalmente
`PersonId`. `NpcRuntimeId` permanece adequado para execução transitória, lookup
runtime, projeção carregada e estado local de agente, mas não para identidade
histórica persistente. Essa regra não exige migrar o `OrganizationRuntime`
atual; ela impede apenas que seu modelo local seja tomado como contrato
canônico futuro.

Save/load deve preservar estado suficiente para que:

```text
continuar normalmente a partir de uma fronteira T
e
salvar em T → carregar → continuar com os mesmos inputs
```

produzam o mesmo futuro autoritativo. Isso inclui todo estado que influencia
decisões futuras, inclusive estado ou contexto de aleatoriedade e
sequências/allocators quando forem semanticamente necessários.

Uma seed inicial isolada não é estado suficiente para continuação.

Para mundos que utilizem a fundação física, topologia espacial, identidades e
condições mutáveis de passagens, posição factual em trânsito, plano/progresso
autoritativo e Knowledge relevante também devem poder ser preservados ou
reconstruídos sem depender de GameObjects, caches ou diagnostics. Isso não
define agora um formato de save ou replay para a viagem civil.

Quando uma Person possui estado individual rico, a persistência deve distinguir
claramente:

```text
Person-only
→ não há estado individual rico

Person + rich state unloaded
→ o estado rico continua existindo, apenas não está carregado
```

Estado rico serializado não deve ser tratado como `Person-only`. A forma de
armazenar esse estado fora do `NpcRuntime` permanece uma decisão futura.

O snapshot deve ser feito preferencialmente em limite consistente entre
ticks/transações.

---

## 92. Save != Replay != History

**DECIDIDO**

- Save — estado necessário para continuar a simulação.
- Reconstrução histórica/replay — recuperar o estado autoritativo em uma
  fronteira simulada por execução determinística compatível e/ou estados
  preservados, com os inputs externos relevantes.
- History — registro seletivo do que merece retenção.

**Garantia de produto:** qualquer fronteira temporal realmente simulada desde
a primeira deve permitir reconstruir o estado autoritativo correspondente e
criar um fork independente que possa continuar dali. Isso inclui `World Truth`
e os demais estados autoritativos necessários para continuidade, com todas as
consequências acumuladas de decisões autônomas, inputs externos e mutações de
domínio até aquela fronteira. A garantia não se estende ao interior da
backstory gerada anterior ao início da simulação.

Uma serraria criada no dia simulado 200 integra um fork da fronteira do dia
205 e não um fork da fronteira do dia 199. A mesma regra se aplica à criação,
alteração e destruição de qualquer estrutura ou fato autoritativo, inclusive
cidades, POIs, construções, estradas, recursos, organizações, empregos e
propriedades. Reconstruir apenas a lista de decisões externas sem seus efeitos
autoritativos não satisfaz a garantia.

Replay, persistência e checkpoints são mecanismos técnicos para cumprir essa
garantia, não novas fontes de verdade histórica. Checkpoints podem acelerar a
reconstrução; sua retenção ou reciclagem não pode eliminar a possibilidade de
fork em uma fronteira realmente simulada. A reconstrução deve respeitar as
semânticas compatíveis de simulação e conteúdo vigentes no período histórico.
O contrato técnico exato de armazenamento, replay, migração e retenção fica
para o consumidor de persistência.

Determinismo e save continuation, isoladamente, não implementam essa garantia.
`History`, eventos e snapshots diagnósticos não substituem o estado
autoritativo nem exigem event sourcing universal. Decisões autônomas podem ser
reproduzidas deterministicamente sem registro individual de cada escolha,
desde que o estado reconstruído seja o mesmo na fronteira escolhida.

O projeto não é event-sourced por padrão.

---

# Parte XXIII — Anti-patterns e abstrações proibidas prematuramente

## 93. Não criar frameworks universais sem necessidade concreta

**DECIDIDO**

Evitar por padrão:

- `WorldEntity` universal;
- `KnowledgeGraph` universal;
- generic Activity engine;
- generic Rules engine;
- generic Command-policy engine gigante;
- Quest framework;
- Behavior Tree framework como infraestrutura central;
- `GoalSystem` universal;
- `Power` global absoluto;
- `UniversalContract` para todos os vínculos;
- `BuildingComponent` universal;
- interfaces vazias para cada nome de entidade;
- event sourcing como default;
- relation graph genérico que apaga invariantes de domínio.

É permitido reutilizar mecanismos neutros, como identidade tipada estável,
registros de ciclo de vida, histórico de relações, índices ativos, stale
guards, snapshots determinísticos e caches reconstruíveis. Isso não autoriza
criar uma `Organization` base/class/store universal apenas por conveniência.

Preferir:

```text
shared mechanisms
        + domain-specific semantics
```

em vez de:

```text
universal entity abstraction
```

Composição reutiliza primitives estáveis; algoritmos especializados continuam específicos de cada domínio.

---

## 94. Não simular detalhe sem consumidor

**DECIDIDO / DIREÇÃO**

Evitar antecipadamente:

- cada refeição;
- cada golpe de batalha ampla;
- cada tijolo;
- cada contato infeccioso;
- genética detalhada;
- meteorologia física completa;
- rotina completa de toda população;
- runtime rico para todo habitante.

O detalhe entra quando altera decisões relevantes ou é exigido pelo produto.

---

## 95. Performance: medir antes de mudar paradigma

**DECIDIDO / DIREÇÃO**

Ordem preferida:

```text
measure
→ indexes / better algorithms / fewer allocations
→ aggregation
→ active/dormant scheduling
→ parallelize pure work
→ data-oriented structures where needed
→ native/Rust/C++ only for proven hotspot
```

Não migrar para ECS/DOTS só por antecipação.

---

# Parte XXIV — Exemplos compostos

## 96. Sucessão política emergente

```text
King dies
→ factual Person death
→ institution eventually recognizes vacancy
→ A and B are factual candidates
→ both create/hold succession claims
→ Council recognizes A
→ Church recognizes B
→ House Lion officially supports A
→ several House Lion members personally support B
→ General knows old information and supports A
→ Merchant Guild knows current recognition and supports B
→ legitimacy assessments differ by perspective
→ Council selects A
→ coronation scheduled for day 110
→ before coronation A may die/lose conditions/be challenged
→ execution at coronation revalidates truth
```

Se B tiver apoio suficiente para resistir:

```text
contested political outcome
→ evaluate whether resistance is worthwhile
→ negotiation / refusal / sanction / faction dispute
→ possible schism
→ possible mobilization
→ possible armed conflict
```

Nunca `king died => StartCivilWar()`.

---

## 97. Cisma e continuidade alegada

```text
Year 100: Order F exists
Year 500: structural schism

F terminates as unified organization
A created with new ID
B created with new ID

A claims: “we are the 500-year-old true Order”
B claims: “we are the 500-year-old true Order”
```

Truth:

```text
A predecessor = F
B predecessor = F
```

Political recognition may differ by institution.

---

## 98. GM injeta fato sem escrever roteiro inteiro

```text
GM Level 2:
“Roberto morreu de epidemia.”

World Truth:
Roberto dead

Simulation consequences:
- no further action
- estate may be opened
- institutions may learn later
- vacancy may be recognized later
- family knowledge/memory may propagate
- claims may appear
```

Se o GM usar Level 3:

```text
“Roberto morreu e toda a fazenda passou para Helena.”
```

Morte e transferência são autorizadas, mas o restante das consequências continua emergente.

---

## 99. Ocupação sem anexação

```text
City Stonehill

legal jurisdiction = Kingdom A
property ownership = House M
military control = Rebel Army
local allegiance = mixed
```

Uma batalha pode mudar `control` sem alterar imediatamente `jurisdiction` ou `ownership`.

Tratado posterior pode reconhecer anexação.

---

## 100. Informação velha sem mentira

```text
Day 100:
Council recognizes Arthur.
Maria observes it.

Day 110:
Council revokes Arthur and recognizes Beatriz.
Maria is traveling and does not learn it.

Day 115:
Maria still acts based on old knowledge.
```

Não é necessário sistema de propaganda para produzir erro decisório.

---

# Parte XXV — Questões deliberadamente abertas

## 101. Open decisions

Estas perguntas não devem receber resposta implícita sem nova definição:

1. **Copropriedade** — porcentagens, joint ownership, usufruto etc.
2. **Polity/Realm concreta** — estrutura final e relação com Institution/Faction.
3. **Cidadania/súdito** — modelo exato e critérios de aquisição/perda.
4. **Ideologia/religião/cultura completas** — somente distinções necessárias já estão definidas.
5. **Rumor/mentira deliberada** — Knowledge suporta erro/staleness; propaganda ativa é posterior.
6. **Memória detalhada** — fórmula de decay e classes exatas ainda não definidas.
7. **Governança genérica** — não criar engine universal antes de casos concretos de líder/conselho/voto.
8. **Guerra estratégica completa** — a semântica constitucional de conflito,
   forças, batalha, controle, occupation, goals e término está definida acima;
   a implementação integrada e seus consumidores concretos permanecem
   deferidos até existir uma Phase própria.
9. **Government/constitution framework** — não generalizar a partir da política de sucessão apenas.
10. **Título/social status** — contratos finais dependem de use cases.

---

# Parte XXVI — Checklist para novas features

## 102. Perguntas arquiteturais obrigatórias

Antes de propor um novo sistema, perguntar:

### Truth

- Qual é a World Truth autoritativa?
- Quem possui a mutação?
- É store próprio, relation store ou estado derivado?

### Knowledge

- Quem precisa saber disso para decidir?
- Pode estar desconhecido, stale ou incorreto?
- Planning está lendo truth secretamente?

### Interpretation

- Saber o fato determina a posição ou existe interpretação/norma?

### Identity

- A identidade precisa sobreviver ao tick, morte, renomeação ou cisma?
- Estamos usando display text como ID por acidente?

### Decision

- Quem decide?
- Possui autoridade formal ou apenas influência?
- É decisão individual ou coletiva?
- Como a governança transforma posições individuais em posição oficial?

### Execution

- Qual domain system executa?
- Revalida truth?
- Possui stale guards?
- A aplicação precisa ser atômica?

### History

- Esse acontecimento merece retenção permanente?
- Pode originar memória duradoura?
- É history ou apenas diagnostic trace?

### Scale

- Precisa ser individual ou pode ser agregado?
- Named actors podem coexistir com aggregate participants?

### Configuration

- É Policy, Parameter ou Content?
- Estamos hardcodando uma característica de setting?

### GM/API

- É Request normal, Declare, ou authored outcome?
- Autoridade está quebrando invariantes estruturais?

### Scope

- Estamos criando uma primitive porque existe mais de um consumidor real?
- Existe framework genérico sendo criado só por antecipação?

---

# Parte XXVII — Resumo executivo dos invariantes

## 103. Constituição curta

```text
WORLD TRUTH != KNOWLEDGE
WORLD TRUTH != INSTITUTIONAL RECOGNITION
KNOWLEDGE != MEMORY
KNOWLEDGE != INTERPRETATION
APPRAISAL != REACTION != RELATIONSHIP
REACTION != SUPPORT != POLITICAL POSITION
TARGET != PERCEIVED ATTRIBUTION
NORM != INTERPRETATION != ENFORCEMENT

QUERY / EVALUATION DOES NOT MUTATE
DECISION != EXECUTION
EXECUTION REVALIDATES TRUTH
RECORDING DOES NOT MUTATE OR CONSUME RNG
EVENT != TRUTH

EXISTENCE != REPRESENTATION != PROCESSING
PERSON-ONLY != DORMANT
MATERIALIZATION != ACTIVITY
ACTIVE / DORMANT / LOADED / UNLOADED != POPULATION CHANGE
PERSON IDENTITY IS NOT HUMAN-ONLY
CALENDAR OCCURRENCE != DOMAIN EFFECT

INITIAL CONFIGURED WORLD → COMPLETE WORLD TRUTH BEFORE FIRST SIMULATED BOUNDARY
GENERATED BACKSTORY != SIMULATED HISTORY
INITIAL GENERATED STATE != IMMUTABLE WORLD
INITIAL GENERATION != RUNTIME MUTATION AUTHORITY
RUNTIME-CREATED WORLD STRUCTURE = NORMAL WORLD TRUTH
EVERY SIMULATED HISTORICAL BOUNDARY → RECONSTRUCTABLE AUTHORITATIVE STATE AND INDEPENDENT FORK

AUTHORITATIVE DETERMINISM:
same compatible version + authoritative state + effective configuration/content
+ deterministic randomness state + logically ordered external commands
→ same semantic authoritative results at the same logical boundaries on all supported hosts

EFFECTIVE CONFIGURATION = RESOLVED POLICY/PARAMETER COMPOSITION
EFFECTIVE CONFIGURATION != GIANT CONTENT SNAPSHOT
AUTHORING SOURCE != RESOLVED EFFECTIVE AUTHORITY
EFFECTIVE CALENDAR = AUTHORITATIVE EXECUTION INPUT
CONFIGURATION + CALENDAR IMMUTABLE PER NORMAL EXECUTION
HOST COMPOSITION CHOOSES HOW, NOT WHAT POLICY IS
AVAILABLE != ENABLED != AUTONOMOUS != INSTANTIATED
ENABLED → AVAILABLE AT EXECUTION BOUNDARY, OR EXPLICIT LAZY LOADING
AUTONOMOUS = ORIGIN OF NEW DISCRETIONARY BEHAVIOR
PROVIDER REGISTRATION != ENABLEMENT
ENABLED CAPABILITY WITHOUT HOST SUPPORT → COMPOSITION FAILURE
SIMULATIONMODULESET LOCAL/LEGACY != CANONICAL ENABLEMENT AUTHORITY
ECONOMY != MERCHANT != MERCHANT AUTONOMY
NEW DISCRETIONARY TRADE REPOSITIONING = AUTONOMY
EXISTING COMMERCIAL COMMITMENT EXECUTION != AUTOMATIC AUTONOMY
MINIMUM PROFIT THRESHOLD → MERCHANT/JOB CONTENT
COMMERCIAL KNOWLEDGE POLICY → EFFECTIVE DOMAIN CONFIGURATION

CAUSAL RANDOMNESS ISOLATION:
semantic world-state change → downstream result may change
incidental unrelated RNG consumption → downstream result should not change

SEMANTIC ORDERING:
causal processing order must be semantic or deterministic;
incidental host collection order is not a world rule.

POPULATION AGGREGATE → PERSON
PERSON-ONLY | RICH STATE LOADED/UNLOADED
RICH STATE → ACTIVE/DORMANT PROCESSING
PERSON != NPCRUNTIME
RESIDENCE != PRESENCE
SIMULATION LOCATION != RENDERING COORDINATE
CONTAINMENT != SAME HEX != CONNECTIVITY
HEX ADJACENCY != TRAVERSABILITY
MOVING ENTITY != LOCATION
DISTANCE != TRAVEL TIME
SHORTEST PATH != FASTEST PATH != SAFEST PATH
ROUTE PLANNING USES KNOWLEDGE
TRAVERSAL EXECUTION REVALIDATES WORLD TRUTH
BATTLE != BATTLE LOCATION != BATTLE AFTERMATH
BATTLE ENDED != BATTLEFIELD MATERIAL STATE ENDED
BATTLEFIELD MATERIAL STATE != HISTORICAL MEMORY
CONTENT DEFINITION != CURRENT SPATIAL WORLD STATE

ORGANIZATION != FACTION != INSTITUTION != POLITY
ORGANIZATIONRUNTIME LOCAL/LEGACY != CANONICAL ORGANIZATION FOUNDATION
PERSISTENT INDIVIDUAL RELATION → PERSONID
NPCRUNTIMEID → TRANSIENT RUNTIME REPRESENTATION
MEMBERSHIP != OFFICE HOLDING != SUPPORT != ALLEGIANCE != CITIZENSHIP != AUTHORITY

AUTHORITY != INFLUENCE
AFFILIATION != SUPPORT != OBEDIENCE
COLLECTIVE POSITION != SUM OF MEMBER POSITIONS
POLITICAL SELECTION != ASSUMPTION OF OFFICE

IDENTITY != CLAIMED CONTINUITY
SCHISM → NEW IDS FOR ALL SUCCESSOR FACTIONS
RENAME → SAME ID
SECESSION WITH ORIGINAL CONTINUITY → ORIGINAL ID SURVIVES
MERGE INTO NEW ENTITY → NEW ID
ABSORPTION → ABSORBER SURVIVES, ABSORBED ENDS

OWNERSHIP != CUSTODY != CONTROL
OWNERSHIP != JURISDICTION != CONTROL != ALLEGIANCE

GENEALOGY != RELATIONSHIP != HOUSEHOLD != DYNASTY
OFFICE != TITLE != SOCIAL STATUS

HOSTILITY != CONFLICT
CONFLICT != WAR
WAR != BATTLE
BATTLE RESULT != WAR RESULT
BATTLE != TERRITORIAL CONTROL
COMMAND != LOYALTY != ALLEGIANCE
MANPOWER SOURCE != COMMAND
SUPPLY != FUNDING / PAY

EVENT != MEMORY != HISTORY
SIGNIFICANT REACTION → AUTHORITATIVE PERSISTENT STATE
REACTION HISTORY → CURRENT VIEW IS DERIVABLE, NOT A SECOND TRUTH
UNKNOWN ATTRIBUTION != UNKNOWN PERSON ID
NO KNOWLEDGE OF RESPONSIBLE ACTOR != NO REACTION TO SUFFERED CONSEQUENCE
REACTION != AUTOMATIC RELATIONSHIP PROJECTION
CHECKPOINT C1 → SOCIAL APPRAISAL FOUNDATION
CHECKPOINT C2 → CRIME/JUSTICE VERTICAL SLICE
CHECKPOINT C3 → PERSISTENT RELATIONSHIP PROJECTION, DEFERRED
SAVE != REPLAY != HISTORY
CHECKPOINT != HISTORICAL TRUTH

FORK FROM A SIMULATED BOUNDARY:
reconstruct World Truth + authoritative continuation state at T;
all domain consequences through T participate;
generated backstory before simulation start is outside the fork guarantee.

SAVE CONTINUATION:
save at T + same future inputs → same future authoritative state
initial seed alone != sufficient continuation state

EXTERNAL COMMANDS ARE DETERMINISTIC INPUT:
order, logical boundary, payload and authority matter;
wall-clock arrival alone does not define causality.

GM MAY OVERRIDE PLAUSIBILITY
GM MAY ASSERT FACTS/OUTCOMES
GM MUST NOT BREAK STRUCTURAL INVARIANTS
PLAYER DECISION AUTHORITY != OMNISCIENT INFORMATION AUTHORITY
IN-WORLD DIVINITY != GM / EXTERNAL AUTHORITY

AI/NLP TRANSLATES TO VALIDATED WORLD COMMANDS;
IT DOES NOT BECOME AN UNCONTROLLED AUTHOR OF WORLD TRUTH.
```

---

# Appendix A — Codex / Architecture Lab usage

## A.1 Read-only architecture discussion

Ao usar Codex para análise arquitetural, a instrução recomendada é:

```text
Read docs/SIMULATION_ARCHITECTURE.md first.
Read the relevant current PHASE*_STATE.md and current canonical code.
Do not modify files.
Analyze the requested design against the conceptual invariants.
Distinguish:
- already decided architecture;
- current implementation constraints;
- future direction;
- genuinely open product decisions.
Do not invent an implementation solely because it would be convenient.
```

## A.2 Implementação

Antes de implementar mudança semântica:

```text
1. Read SIMULATION_ARCHITECTURE.md.
2. Read current phase state.
3. Inspect actual canonical code.
4. Identify affected invariants.
5. If implementation would contradict a DECIDED invariant, stop for explicit architecture review.
6. If document only states DIRECTION, choose the smallest implementation justified by current consumers.
7. Never silently turn an OPEN question into a permanent architecture decision.
```

---

# Appendix B — Anti-scope-creep reminder

O projeto busca **emergência com continuidade**, não simulação total.

Uma feature deve existir porque altera decisões, persistência, conhecimento, autoridade, recursos, história ou causalidade relevante — não apenas porque “seria realista”.

A melhor arquitetura continua sendo a menor estrutura que preserva as diferenças que realmente mudam o mundo.
