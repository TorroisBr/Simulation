using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldSimulationDemo : MonoBehaviour
{
    [Header("Configuração da simulação")]
    [SerializeField] private int seed = 42;
    [SerializeField] private int daysToSimulate = 10;

    private System.Random rng;

    private void Start()
    {
        rng = new System.Random(seed);
        RunSimulation();
    }

    private void RunSimulation()
    {
        WorldState world = new WorldState
        {
            Day = 1,
            Danger = 45,
            Law = 40,
            OrderActivity = 60,
            RumorHeat = 25
        };

        Npc claudio = new Npc
        {
            Id = "claudio_vendedor",
            Name = "Cláudio",
            Role = "Vendedor",
            Courage = 4,
            Greed = 6,
            Caution = 7,
            Loyalty = 5,
            Resources = 2
        };

        claudio.Tags.Add("mercador");
        claudio.Tags.Add("cidade");
        claudio.Tags.Add("sabe_segredo");
        claudio.Status.Add("knows_secret");

        Debug.Log("===== INÍCIO DA SIMULAÇÃO =====");

        for (int i = 0; i < daysToSimulate; i++)
        {
            Debug.Log("");
            Debug.Log($"===== DIA {world.Day} =====");
            Debug.Log(GetStateText(claudio, world));

            SimulateNpcTurn(claudio, world);
            SimulateFactionPressure(claudio, world);

            AdvanceWorld(world, claudio);
        }

        Debug.Log("===== FIM DA SIMULAÇÃO =====");
        Debug.Log(GetStateText(claudio, world));
    }

    private void SimulateNpcTurn(Npc npc, WorldState world)
    {
        List<CandidateAction> actions;

        if (npc.HasStatus("kidnapped"))
            actions = BuildKidnappedNpcActions(npc, world);
        else
            actions = BuildFreeNpcActions(npc, world);

        LogActionTable($"Ações possíveis de {npc.Name}", actions);

        CandidateAction chosen = PickWeighted(actions);

        if (chosen == null)
        {
            Debug.Log($"{npc.Name} não fez nada relevante hoje.");
            return;
        }

        Debug.Log($"AÇÃO DE {npc.Name}: {chosen.Label}");
        chosen.Resolve?.Invoke();
    }

    private List<CandidateAction> BuildFreeNpcActions(Npc npc, WorldState world)
    {
        List<CandidateAction> actions = new List<CandidateAction>();

        // Abrir loja / manter rotina
        {
            int weight = 30;
            List<string> reasons = new List<string> { "base 30" };

            int greedBonus = npc.Greed * 2;
            weight += greedBonus;
            reasons.Add($"+ ganância {greedBonus}");

            int lawBonus = world.Law / 5;
            weight += lawBonus;
            reasons.Add($"+ lei/estabilidade {lawBonus}");

            int dangerPenalty = world.Danger / 5;
            weight -= dangerPenalty;
            reasons.Add($"- perigo {dangerPenalty}");

            if (npc.HasStatus("knows_secret"))
            {
                int orderPenalty = world.OrderActivity / 8;
                weight -= orderPenalty;
                reasons.Add($"- medo da Ordem {orderPenalty}");
            }

            if (npc.HasStatus("hidden"))
            {
                weight -= 40;
                reasons.Add("- está escondido 40");
            }

            if (npc.HasStatus("left_town"))
            {
                weight -= 100;
                reasons.Add("- saiu da cidade");
            }

            AddAction(actions, "open_shop", "Abrir a banca e agir normalmente", weight, reasons, () =>
            {
                npc.RemoveStatus("hidden");
                npc.Status.Add("public_today");
                npc.Resources += 1;

                Debug.Log($"{npc.Name} abriu a banca. Recursos +1.");

                if (RollPercent(20))
                {
                    world.RumorHeat += 5;
                    Debug.Log("Clientes comentaram boatos estranhos. Rumores +5.");
                }
            });
        }

        // Procurar ajuda da guarda
        {
            int weight = 8;
            List<string> reasons = new List<string> { "base 8" };

            int cautionBonus = npc.Caution * 3;
            weight += cautionBonus;
            reasons.Add($"+ cautela {cautionBonus}");

            int dangerBonus = world.Danger / 3;
            weight += dangerBonus;
            reasons.Add($"+ perigo {dangerBonus}");

            if (npc.HasStatus("knows_secret"))
            {
                weight += 20;
                reasons.Add("+ sabe segredo 20");
            }

            if (npc.HasStatus("guarded"))
            {
                weight -= 50;
                reasons.Add("- já está protegido");
            }

            if (npc.HasStatus("left_town"))
            {
                weight -= 100;
                reasons.Add("- saiu da cidade");
            }

            AddAction(actions, "seek_guards", "Procurar ajuda da guarda", weight, reasons, () =>
            {
                int chance = 35 + (world.Law / 2) + (npc.Loyalty * 2) - (world.OrderActivity / 3);

                ResolveCheck(
                    "conseguir proteção da guarda",
                    chance,
                    onSuccess: () =>
                    {
                        npc.Status.Add("guarded");
                        world.Law += 5;
                        Debug.Log("A guarda acreditou em Cláudio. Ele está protegido. Lei +5.");
                    },
                    onPartial: () =>
                    {
                        npc.Status.Add("guarded");
                        world.RumorHeat += 10;
                        Debug.Log("A guarda ajudou, mas o caso virou fofoca. Protegido, Rumores +10.");
                    },
                    onFailure: () =>
                    {
                        world.OrderActivity += 5;
                        Debug.Log("A guarda ignorou Cláudio. A Ordem percebeu o movimento. Atividade da Ordem +5.");
                    }
                );
            });
        }

        // Investigar símbolo/segredo
        {
            int weight = 12;
            List<string> reasons = new List<string> { "base 12" };

            int courageBonus = npc.Courage * 3;
            weight += courageBonus;
            reasons.Add($"+ coragem {courageBonus}");

            if (npc.HasStatus("knows_secret"))
            {
                weight += 15;
                reasons.Add("+ sabe segredo 15");
            }

            int dangerPenalty = world.Danger / 4;
            weight -= dangerPenalty;
            reasons.Add($"- perigo {dangerPenalty}");

            if (npc.HasStatus("marked_by_order"))
            {
                weight -= 20;
                reasons.Add("- marcado pela Ordem 20");
            }

            if (npc.HasStatus("left_town"))
            {
                weight -= 100;
                reasons.Add("- saiu da cidade");
            }

            AddAction(actions, "investigate", "Investigar o símbolo estranho", weight, reasons, () =>
            {
                int chance = 40 + (npc.Courage * 5) - (world.Danger / 4);

                ResolveCheck(
                    "investigar símbolo",
                    chance,
                    onSuccess: () =>
                    {
                        world.Events.Add("claudio_descobriu_rota_da_ordem");
                        world.RumorHeat += 10;
                        Debug.Log("Cláudio descobriu uma rota usada pela Ordem. Novo evento criado. Rumores +10.");
                    },
                    onPartial: () =>
                    {
                        npc.Status.Add("marked_by_order");
                        world.RumorHeat += 5;
                        Debug.Log("Cláudio descobriu algo, mas foi visto. Marcado pela Ordem. Rumores +5.");
                    },
                    onFailure: () =>
                    {
                        npc.Status.Add("marked_by_order");
                        world.Danger += 5;
                        Debug.Log("Cláudio mexeu onde não devia. Marcado pela Ordem. Perigo +5.");
                    }
                );
            });
        }

        // Esconder-se
        {
            int weight = 5;
            List<string> reasons = new List<string> { "base 5" };

            int cautionBonus = npc.Caution * 4;
            weight += cautionBonus;
            reasons.Add($"+ cautela {cautionBonus}");

            int dangerBonus = world.Danger / 4;
            weight += dangerBonus;
            reasons.Add($"+ perigo {dangerBonus}");

            int orderBonus = world.OrderActivity / 4;
            weight += orderBonus;
            reasons.Add($"+ atividade da Ordem {orderBonus}");

            if (npc.HasStatus("marked_by_order"))
            {
                weight += 20;
                reasons.Add("+ marcado pela Ordem 20");
            }

            weight -= npc.Courage;
            reasons.Add($"- coragem {npc.Courage}");

            if (npc.HasStatus("hidden"))
            {
                weight -= 25;
                reasons.Add("- já está escondido");
            }

            if (npc.HasStatus("left_town"))
            {
                weight -= 100;
                reasons.Add("- saiu da cidade");
            }

            AddAction(actions, "hide", "Fechar a banca e se esconder", weight, reasons, () =>
            {
                npc.Status.Add("hidden");
                npc.RemoveStatus("public_today");
                npc.Resources = Mathf.Max(0, npc.Resources - 1);

                Debug.Log($"{npc.Name} se escondeu. Recursos -1.");
            });
        }

        // Avisar aventureiros
        {
            int weight = 10;
            List<string> reasons = new List<string> { "base 10" };

            int loyaltyBonus = npc.Loyalty * 4;
            weight += loyaltyBonus;
            reasons.Add($"+ lealdade {loyaltyBonus}");

            int orderBonus = world.OrderActivity / 5;
            weight += orderBonus;
            reasons.Add($"+ ameaça da Ordem {orderBonus}");

            int cautionPenalty = npc.Caution * 2;
            weight -= cautionPenalty;
            reasons.Add($"- cautela {cautionPenalty}");

            if (!npc.HasStatus("knows_secret"))
            {
                weight -= 100;
                reasons.Add("- não sabe segredo");
            }

            if (npc.HasStatus("players_contacted"))
            {
                weight -= 60;
                reasons.Add("- já procurou aventureiros");
            }

            if (npc.HasStatus("left_town"))
            {
                weight -= 100;
                reasons.Add("- saiu da cidade");
            }

            AddAction(actions, "warn_players", "Procurar aventureiros para contar o segredo", weight, reasons, () =>
            {
                npc.Status.Add("players_contacted");
                world.Events.Add("claudio_procura_aventureiros");
                world.RumorHeat += 10;

                Debug.Log("Cláudio decidiu procurar aventureiros. Novo gancho criado. Rumores +10.");
            });
        }

        // Fugir da cidade
        {
            int weight = 3;
            List<string> reasons = new List<string> { "base 3" };

            int cautionBonus = npc.Caution * 2;
            weight += cautionBonus;
            reasons.Add($"+ cautela {cautionBonus}");

            int dangerBonus = world.Danger / 3;
            weight += dangerBonus;
            reasons.Add($"+ perigo {dangerBonus}");

            int orderBonus = world.OrderActivity / 3;
            weight += orderBonus;
            reasons.Add($"+ atividade da Ordem {orderBonus}");

            int resourcePenalty = npc.Resources * 3;
            weight -= resourcePenalty;
            reasons.Add($"- recursos disponíveis {resourcePenalty}");

            if (npc.HasStatus("left_town"))
            {
                weight -= 100;
                reasons.Add("- já saiu da cidade");
            }

            AddAction(actions, "flee", "Tentar fugir da cidade", weight, reasons, () =>
            {
                int chance = 50 + (npc.Resources * 10) + (npc.Caution * 3) - (world.OrderActivity / 3);

                ResolveCheck(
                    "fugir da cidade",
                    chance,
                    onSuccess: () =>
                    {
                        npc.Status.Add("left_town");
                        npc.RemoveStatus("hidden");
                        npc.RemoveStatus("public_today");
                        Debug.Log("Cláudio fugiu da cidade. Ele não está mais disponível normalmente.");
                    },
                    onPartial: () =>
                    {
                        npc.Status.Add("on_road");
                        npc.Status.Add("marked_by_order");
                        Debug.Log("Cláudio saiu às pressas, mas foi seguido. Está na estrada e marcado pela Ordem.");
                    },
                    onFailure: () =>
                    {
                        npc.Status.Add("marked_by_order");
                        world.OrderActivity += 10;
                        Debug.Log("A fuga falhou. A Ordem percebeu. Atividade da Ordem +10.");
                    }
                );
            });
        }

        return actions;
    }

    private List<CandidateAction> BuildKidnappedNpcActions(Npc npc, WorldState world)
    {
        List<CandidateAction> actions = new List<CandidateAction>();

        // Tentar escapar
        {
            int weight = 20 + (npc.Courage * 5);
            List<string> reasons = new List<string>
            {
                "base 20",
                $"+ coragem {npc.Courage * 5}"
            };

            AddAction(actions, "escape", "Tentar escapar do cativeiro", weight, reasons, () =>
            {
                int chance = 30 + (npc.Courage * 6) - (world.OrderActivity / 4);

                ResolveCheck(
                    "escapar",
                    chance,
                    onSuccess: () =>
                    {
                        npc.RemoveStatus("kidnapped");
                        npc.Status.Add("escaped");
                        npc.Status.Add("hidden");
                        world.RumorHeat += 15;
                        Debug.Log("Cláudio escapou do cativeiro. Agora está escondido. Rumores +15.");
                    },
                    onPartial: () =>
                    {
                        npc.Status.Add("wounded");
                        world.RumorHeat += 10;
                        Debug.Log("Cláudio quase escapou, mas se feriu. Rumores +10.");
                    },
                    onFailure: () =>
                    {
                        npc.Status.Add("wounded");
                        world.OrderActivity += 5;
                        Debug.Log("A tentativa falhou. Cláudio foi punido. Atividade da Ordem +5.");
                    }
                );
            });
        }

        // Negociar com captores
        {
            int weight = 10 + (npc.Caution * 3) + (npc.Greed * 2);
            List<string> reasons = new List<string>
            {
                "base 10",
                $"+ cautela {npc.Caution * 3}",
                $"+ instinto de barganha {npc.Greed * 2}"
            };

            AddAction(actions, "bargain", "Tentar negociar com a Ordem", weight, reasons, () =>
            {
                int chance = 35 + (npc.Caution * 4) + (npc.Resources * 5);

                ResolveCheck(
                    "negociar com a Ordem",
                    chance,
                    onSuccess: () =>
                    {
                        npc.RemoveStatus("kidnapped");
                        npc.Status.Add("owes_order");
                        npc.Status.Add("hidden");
                        Debug.Log("Cláudio foi solto, mas agora deve algo à Ordem.");
                    },
                    onPartial: () =>
                    {
                        world.Events.Add("claudio_revelou_parte_do_segredo");
                        world.OrderActivity += 10;
                        Debug.Log("Cláudio comprou tempo, mas revelou parte do segredo. Atividade da Ordem +10.");
                    },
                    onFailure: () =>
                    {
                        npc.Status.Add("wounded");
                        Debug.Log("A Ordem recusou a barganha. Cláudio está ferido.");
                    }
                );
            });
        }

        // Esperar oportunidade
        {
            int weight = 10 + (npc.Caution * 4);
            List<string> reasons = new List<string>
            {
                "base 10",
                $"+ cautela {npc.Caution * 4}"
            };

            AddAction(actions, "wait", "Esperar uma oportunidade melhor", weight, reasons, () =>
            {
                npc.Status.Add("still_captive");
                world.RumorHeat += 5;

                Debug.Log("Cláudio esperou. Pequenos rumores sobre seu desaparecimento começam a circular. Rumores +5.");
            });
        }

        return actions;
    }

    private void SimulateFactionPressure(Npc npc, WorldState world)
    {
        if (npc.HasStatus("left_town"))
        {
            Debug.Log("A Ordem não age diretamente contra Cláudio porque ele saiu da cidade.");
            return;
        }

        List<CandidateAction> actions = new List<CandidateAction>();

        // Nada grave acontece
        AddAction(actions, "nothing", "Nada grave acontece", 30, new List<string> { "base 30" }, () => 
            {
                Debug.Log("A Ordem não fez nenhum movimento visível contra Cláudio hoje.");
            }
        );

        // Espionar Cláudio
        {
            int weight = 10;
            List<string> reasons = new List<string> { "base 10" };

            int orderBonus = world.OrderActivity / 3;
            weight += orderBonus;
            reasons.Add($"+ atividade da Ordem {orderBonus}");

            if (npc.HasStatus("knows_secret"))
            {
                weight += 15;
                reasons.Add("+ alvo sabe segredo 15");
            }

            if (npc.HasStatus("public_today"))
            {
                weight += 15;
                reasons.Add("+ apareceu em público 15");
            }

            if (npc.HasStatus("hidden"))
            {
                weight -= 10;
                reasons.Add("- alvo escondido 10");
            }

            if (npc.HasStatus("guarded"))
            {
                weight -= 20;
                reasons.Add("- protegido pela guarda 20");
            }

            AddAction(actions, "spy", "A Ordem espiona Cláudio", weight, reasons, () =>
            {
                int chance = 50 + (world.OrderActivity / 2) - (world.Law / 3);

                if (npc.HasStatus("hidden"))
                    chance -= 15;

                if (npc.HasStatus("guarded"))
                    chance -= 20;

                ResolveCheck(
                    "espionar Cláudio",
                    chance,
                    onSuccess: () =>
                    {
                        npc.Status.Add("marked_by_order");
                        Debug.Log("A Ordem conseguiu seguir Cláudio. Ele está marcado.");
                    },
                    onPartial: () =>
                    {
                        world.RumorHeat += 5;
                        Debug.Log("A Ordem conseguiu pistas vagas. Rumores +5.");
                    },
                    onFailure: () =>
                    {
                        world.Law += 5;
                        Debug.Log("A espionagem chamou atenção da guarda. Lei +5.");
                    }
                );
            });
        }

        // Raptar Cláudio
        {
            int weight = 5;
            List<string> reasons = new List<string> { "base 5" };

            int orderBonus = world.OrderActivity / 2;
            weight += orderBonus;
            reasons.Add($"+ atividade da Ordem {orderBonus}");

            if (npc.HasStatus("knows_secret"))
            {
                weight += 35;
                reasons.Add("+ sabe segredo importante 35");
            }

            if (npc.HasStatus("marked_by_order"))
            {
                weight += 20;
                reasons.Add("+ já está marcado 20");
            }

            if (npc.HasStatus("public_today"))
            {
                weight += 15;
                reasons.Add("+ apareceu em público 15");
            }

            if (npc.HasStatus("hidden"))
            {
                weight -= 20;
                reasons.Add("- está escondido 20");
            }

            if (npc.HasStatus("guarded"))
            {
                weight -= 30;
                reasons.Add("- protegido pela guarda 30");
            }

            int lawPenalty = world.Law / 4;
            weight -= lawPenalty;
            reasons.Add($"- presença da lei {lawPenalty}");

            if (npc.HasStatus("kidnapped"))
            {
                weight -= 100;
                reasons.Add("- já foi raptado");
            }

            AddAction(actions, "kidnap", "A Ordem tenta raptar Cláudio", weight, reasons, () =>
            {
                int chance = 35 + (world.OrderActivity / 2) - (world.Law / 2);

                if (npc.HasStatus("guarded"))
                    chance -= 25;

                if (npc.HasStatus("hidden"))
                    chance -= 10;

                ResolveCheck(
                    "raptar Cláudio",
                    chance,
                    onSuccess: () =>
                    {
                        npc.Status.Add("kidnapped");
                        npc.RemoveStatus("public_today");
                        npc.RemoveStatus("hidden");
                        world.Events.Add("claudio_foi_raptado_pela_ordem");
                        world.RumorHeat += 20;

                        Debug.Log("A Ordem raptou Cláudio. Novo gancho criado. Rumores +20.");
                    },
                    onPartial: () =>
                    {
                        npc.Status.Add("marked_by_order");
                        npc.Status.Add("wounded");
                        world.RumorHeat += 15;

                        Debug.Log("Cláudio escapou por pouco, mas está ferido e marcado. Rumores +15.");
                    },
                    onFailure: () =>
                    {
                        world.Law += 10;
                        world.OrderActivity -= 5;

                        Debug.Log("A tentativa de rapto falhou e chamou atenção. Lei +10, Atividade da Ordem -5.");
                    }
                );
            });
        }

        // Comprar informante
        {
            int weight = 8;
            List<string> reasons = new List<string> { "base 8" };

            int orderBonus = world.OrderActivity / 4;
            weight += orderBonus;
            reasons.Add($"+ atividade da Ordem {orderBonus}");

            if (npc.Resources <= 1)
            {
                weight += 15;
                reasons.Add("+ alvo com poucos recursos 15");
            }

            if (world.Law < 40)
            {
                weight += 10;
                reasons.Add("+ lei fraca 10");
            }

            AddAction(actions, "bribe", "A Ordem compra um informante próximo de Cláudio", weight, reasons, () =>
            {
                int chance = 45 + (world.OrderActivity / 3) - (world.Law / 4);

                ResolveCheck(
                    "comprar informante",
                    chance,
                    onSuccess: () =>
                    {
                        npc.Status.Add("informant_nearby");
                        npc.Status.Add("marked_by_order");
                        Debug.Log("A Ordem comprou um informante perto de Cláudio. Ele está marcado.");
                    },
                    onPartial: () =>
                    {
                        world.RumorHeat += 10;
                        Debug.Log("Um informante aceitou dinheiro, mas falou demais. Rumores +10.");
                    },
                    onFailure: () =>
                    {
                        world.RumorHeat += 15;
                        Debug.Log("A tentativa de suborno vazou. Rumores +15.");
                    }
                );
            });
        }

        LogActionTable("Pressões possíveis da Ordem", actions);

        CandidateAction chosen = PickWeighted(actions);

        if (chosen == null)
            return;

        Debug.Log($"AÇÃO DA ORDEM: {chosen.Label}");
        chosen.Resolve?.Invoke();
    }

    private void AdvanceWorld(WorldState world, Npc npc)
    {
        world.Day++;

        npc.RemoveStatus("public_today");

        world.Danger = Mathf.Clamp(world.Danger + rng.Next(-3, 4), 0, 100);
        world.Law = Mathf.Clamp(world.Law + rng.Next(-2, 3), 0, 100);
        world.OrderActivity = Mathf.Clamp(world.OrderActivity, 0, 100);
        world.RumorHeat = Mathf.Clamp(world.RumorHeat - 5, 0, 100);

        if (world.Events.Contains("claudio_foi_raptado_pela_ordem"))
        {
            world.OrderActivity = Mathf.Clamp(world.OrderActivity + 5, 0, 100);
        }

        if (world.Events.Contains("claudio_procura_aventureiros"))
        {
            world.RumorHeat = Mathf.Clamp(world.RumorHeat + 5, 0, 100);
        }

        if (RollPercent(world.RumorHeat / 4))
        {
            world.OrderActivity = Mathf.Clamp(world.OrderActivity + 5, 0, 100);
            Debug.Log("Os rumores alimentaram a movimentação da Ordem. Atividade da Ordem +5.");
        }
    }

    private void AddAction(List<CandidateAction> actions, string id, string label, int weight, List<string> reasons, Action resolve)
    {
        weight = Mathf.Clamp(weight, 0, 999);

        if (weight <= 0)
            return;

        actions.Add(new CandidateAction
        {
            Id = id,
            Label = label,
            Weight = weight,
            Reasons = reasons,
            Resolve = resolve
        });
    }

    private CandidateAction PickWeighted(List<CandidateAction> actions)
    {
        int totalWeight = actions.Sum(a => a.Weight);

        if (totalWeight <= 0)
            return null;

        int roll = rng.Next(1, totalWeight + 1);
        int current = 0;

        Debug.Log($"Rolagem ponderada: {roll}/{totalWeight}");

        foreach (CandidateAction action in actions)
        {
            current += action.Weight;

            if (roll <= current)
                return action;
        }

        return actions[actions.Count - 1];
    }

    private void ResolveCheck(string label, int successChance, Action onSuccess, Action onPartial, Action onFailure)
    {
        successChance = Mathf.Clamp(successChance, 5, 95);

        int roll = rng.Next(1, 101);

        Debug.Log($"Teste: {label}. Chance de sucesso: {successChance}%. Rolagem: {roll}.");

        if (roll <= successChance)
        {
            Debug.Log("Resultado: sucesso.");
            onSuccess?.Invoke();
        }
        else if (roll <= successChance + 20)
        {
            Debug.Log("Resultado: sucesso parcial / complicação.");
            onPartial?.Invoke();
        }
        else
        {
            Debug.Log("Resultado: falha.");
            onFailure?.Invoke();
        }
    }

    private bool RollPercent(int chance)
    {
        chance = Mathf.Clamp(chance, 0, 100);
        return rng.Next(1, 101) <= chance;
    }

    private void LogActionTable(string title, List<CandidateAction> actions)
    {
        if (actions.Count == 0)
        {
            Debug.Log($"{title}: nenhuma ação possível.");
            return;
        }

        int total = actions.Sum(a => a.Weight);

        string text = $"{title}:\n";

        foreach (CandidateAction action in actions)
        {
            float percent = total > 0 ? (action.Weight / (float)total) * 100f : 0f;
            string reasons = string.Join("; ", action.Reasons);

            text += $"- {action.Label}: peso {action.Weight} ({percent:0.0}%) | {reasons}\n";
        }

        Debug.Log(text);
    }

    private string GetStateText(Npc npc, WorldState world)
    {
        string statuses = npc.Status.Count > 0
            ? string.Join(", ", npc.Status)
            : "nenhum";

        string eventsText = world.Events.Count > 0
            ? string.Join(", ", world.Events)
            : "nenhum";

        return
            $"Estado do mundo:\n" +
            $"- Perigo: {world.Danger}\n" +
            $"- Lei: {world.Law}\n" +
            $"- Atividade da Ordem: {world.OrderActivity}\n" +
            $"- Rumores: {world.RumorHeat}\n" +
            $"- Eventos: {eventsText}\n" +
            $"\n" +
            $"Estado de {npc.Name}:\n" +
            $"- Recursos: {npc.Resources}\n" +
            $"- Status: {statuses}";
    }
}