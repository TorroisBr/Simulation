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

## NPC Actions

Fluxo atual:

Status
→ ações válidas
→ Utility
→ sorteio ponderado
→ ação
→ resultado

- baseUtility representa vontade básica.
- StatusWeightModifier altera Utility.
- Utility não representa diretamente porcentagem.
- A Utility final atualmente é usada como peso no sorteio.

## Direção futura

- Ações podem possuir alvo.
- Ações podem falhar.
- Ações podem afetar outro NPC.
- Exemplo: PRENDER tem um NpcRuntime como alvo.
- Mercado será implementado antes de viagem.
- Mercadores devem explorar diferenças de preço entre cidades.

## Código

- Priorizar soluções simples antes de abstrações genéricas.
- Não criar uma classe diferente para cada ação sem necessidade.
- Evitar sistemas excessivamente complexos antes de haver um caso concreto.
