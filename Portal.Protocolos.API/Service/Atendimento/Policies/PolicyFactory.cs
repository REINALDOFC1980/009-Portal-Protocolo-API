namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public static class PolicyFactory
{
    public static List<IServicoPolicy> Criar()
    {
        return
[
            new DefesaPolicy(),
            new CondutorPolicy(),
            new JariPolicy(),
            new CetranPolicy(),
            new RessarcimentoPolicy()
        ];
    }
}
