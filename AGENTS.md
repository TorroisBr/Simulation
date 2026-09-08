# Simulation

Projeto Unity/C# de simulação de mundo inspirada em Dwarf Fortress.

## Escala da simulação

- Um tick atualmente representa 1 dia.
- NPCs executam aproximadamente uma ação relevante por dia.
- Não simular necessidades pequenas como comer/dormir por enquanto.
- A população comum das cidades deve ser abstrata.
- Apenas NPCs relevantes possuem NpcRuntime completo.

## Arquitetura

- ScriptableObjects representam definições estáticas.
- Classes Runtime representam estado mutável da simulação.
- NpcData não deve guardar estado da simulação.
- NpcRuntime guarda status, ação atual, localização, inventário etc.
- SimulationConfigData representa o cenário/mundo configurado: módulos habilitados, cidades, NPCs, ações, status e jobs usados.
- Módulos de simulação são normalizados em runtime; Merchant depende de Economy.
- Módulos atuais: Economy, Merchant e GuardCrime. TravelSystem/TravelActionProvider são core e podem ser usados por sistemas diferentes.

## NPC Actions

Fluxo atual:

Status
→ ações válidas
→ Utility
→ sorteio ponderado
→ NpcActionRuntime
→ tentativa
→ sucesso/falha
→ efeitos de sucesso

- baseUtility representa vontade básica.
- StatusWeightModifier altera Utility.
- Utility não representa diretamente porcentagem.
- A Utility final atualmente é usada como peso no sorteio.
- Utility decide vontade de tentar; baseSuccessChance decide se a tentativa teve sucesso.
- Efeitos de status do executor e do TargetNpc só devem ser aplicados quando a ação tem sucesso.
- Ações contextuais são fornecidas por INpcActionProvider. Ações especiais sem provider habilitado não entram na decisão.
- NpcActionRuntime é a instância concreta extensível da ação e pode carregar TargetNpc, TargetCity, TargetItem, Amount etc.

## Direção futura

- Ações podem possuir alvo.
- Ações podem falhar.
- Ações podem afetar outro NPC.
- Exemplo: PRENDER tem um NpcRuntime como alvo.
- Mercado, viagem e mercadores já existem; mercadores exploram diferenças de preço entre cidades.

## Código

- Priorizar soluções simples antes de abstrações genéricas.
- Não criar uma classe diferente para cada ação sem necessidade.
- Evitar sistemas excessivamente complexos antes de haver um caso concreto.
