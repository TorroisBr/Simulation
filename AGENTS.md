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
- Módulos atuais: Economy, Merchant, Crime e GuardCrime. TravelSystem/TravelActionProvider são core e não são exclusivos de Merchant.
- JusticeSystem mantém mandados e sentenças runtime; mandados são locais por cidade e não ficam em ScriptableObjects.
- PROCURADO é um resumo derivado: deve existir quando o NPC possui pelo menos um mandado ativo.

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
- NpcActionData possui NpcActionCategory para classificar família ampla sem substituir NpcActionType.
- Ações de crime iniciais: ROUBAR, ESCONDER_SE, FUGIR_DA_CIDADE e FUGIR_DA_PRISAO.
- Ações neutras como DESCANSAR, PASSEAR e IR_A_TAVERNA usam NpcActionType.Normal e entram no mesmo sorteio ponderado.
- Utility representa a intenção de tentar uma ação, enquanto Success Chance permanece separado; falhas de fuga aumentam pena e vigilância no estado runtime.

## Direção futura

- Ações podem possuir alvo.
- Ações podem falhar.
- Ações podem afetar outro NPC.
- Exemplo: PRENDER tem um NpcRuntime como alvo.
- Mercado, viagem e mercadores já existem; mercadores exploram diferenças de preço entre cidades.
- NpcJobType representa uma família ampla de comportamento; NpcJobData pode especializar preferências, como preferredTradeItems para jobs Merchant.
- Mercadores usam NpcJobType.Merchant com MerchantBehavior Traveling ou Local. O mercador local compra diretamente de viajantes, mantém estoque e vende ao mercado, sem iniciar viagem comercial.
- MerchantTradePlan pendente acumula urgência apenas enquanto o mercador está parado e ainda precisa viajar; a urgência é resetada ao criar, concluir ou limpar o plano.
- O mercador local representa varejo/intermediação: a venda viajante para mercador local usa preço atacadista, valida margem, reserva de caixa, cidade, estoque e dinheiro próprio do comprador; quando não há comprador válido, MarketRuntime permanece como fallback.
- Prisão usa PrisonSentenceRuntime. Ao cumprir pena, resolve o mandado local e sincroniza PROCURADO conforme outros mandados ativos.

## Observabilidade e cenários

- SimulationLogger filtra Day, NpcAction, Trade, Travel, Crime, Justice, produção, consumo e Market por SimulationConfigData.logSettings; warnings e errors continuam visíveis.
- GeneralTest é o sandbox integrado com duas cidades, mercadores viajantes e locais, guardas, crime e civis.

## Código

- Priorizar soluções simples antes de abstrações genéricas.
- Não criar uma classe diferente para cada ação sem necessidade.
- Evitar sistemas excessivamente complexos antes de haver um caso concreto.
