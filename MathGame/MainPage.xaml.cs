using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MathGame
{
    public partial class MainPage : ContentPage
    {
        private int maxNumber = 10;
        private int pontuacao = 0;
        private int rodadaAtual = 0;
        private const int totalRodadas = 10;

        private int numero1, numero2, operacao;
        private float respostaCerta;
        private DateTime inicioQuestao;

        private CancellationTokenSource timerToken;
        private bool processandoResposta = false;
        private Random random = new Random();

        public MainPage()
        {
            InitializeComponent();
        }

        private async void Operations_SelectedIndexChanged(object sender, EventArgs e)
        {
            string nivel = Operations.SelectedItem as string;
            if (string.IsNullOrEmpty(nivel)) return;

            timerToken?.Cancel();
            processandoResposta = false;
            pontuacao = 0;
            rodadaAtual = 0;

            maxNumber = nivel switch
            {
                "Fácil" => 10,
                "Médio" => 50,
                "Difícil" => 100,
                _ => 10
            };

            MostrarApenasTimer();
            await ContagemRegressiva();

            MostrarTodosControles();
            GerarNovaQuestao();
        }

        private void MostrarApenasTimer()
        {
            lb1.IsVisible = lb2.IsVisible = lb3.IsVisible = false;
            txR.IsVisible = btOK.IsVisible = imgR.IsVisible = false;
            Points.IsVisible = RoundLabel.IsVisible = false;
            TimerLabel.IsVisible = true;
        }

        private void MostrarTodosControles()
        {
            lb1.IsVisible = lb2.IsVisible = lb3.IsVisible = true;
            txR.IsVisible = btOK.IsVisible = imgR.IsVisible = true;
            Points.IsVisible = RoundLabel.IsVisible = TimerLabel.IsVisible = true;
        }

        private async Task ContagemRegressiva()
        {
            for (int i = 3; i >= 1; i--)
            {
                TimerLabel.Text = i.ToString();
                TimerLabel.TextColor = Colors.Blue;
                await Task.Delay(1000);
            }
        }

        private void GerarNovaQuestao()
        {
            txR.Text = "";
            imgR.Source = "question.png";
            btOK.IsEnabled = true;

            numero1 = random.Next(1, maxNumber + 1);
            numero2 = random.Next(1, maxNumber + 1);
            operacao = random.Next(1, 5); // 1=+, 2=-, 3=*, 4=/

            lb1.Text = numero1.ToString();
            lb3.Text = numero2.ToString();

            switch (operacao)
            {
                case 1:
                    lb2.Text = "+";
                    respostaCerta = numero1 + numero2;
                    break;
                case 2:
                    lb2.Text = "-";
                    respostaCerta = numero1 - numero2;
                    break;
                case 3:
                    lb2.Text = "×";
                    respostaCerta = numero1 * numero2;
                    break;
                case 4:
                    lb2.Text = "÷";
                    respostaCerta = (float)numero1 / numero2;
                    break;
            }

            rodadaAtual++;
            RoundLabel.Text = $"{rodadaAtual}/{totalRodadas}";
            Points.Text = $"Pontuação: {pontuacao}";

            inicioQuestao = DateTime.Now;
            IniciarTimer();
        }

        private void IniciarTimer()
        {
            timerToken?.Cancel();
            timerToken = new CancellationTokenSource();

            int tempoRestante = 30;

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (timerToken.IsCancellationRequested || processandoResposta)
                    return false;

                tempoRestante--;
                TimerLabel.Text = $"Tempo: {tempoRestante}s";

                // Muda cor quando tempo está acabando
                TimerLabel.TextColor = tempoRestante <= 10 ? Colors.Red : Colors.Black;

                if (tempoRestante <= 0)
                {
                    MainThread.BeginInvokeOnMainThread(() => TempoEsgotado());
                    return false;
                }

                return true;
            });
        }

        private async void btOK_Clicked(object sender, EventArgs e)
        {
            if (processandoResposta) return;

            processandoResposta = true;
            btOK.IsEnabled = false;
            timerToken?.Cancel();

            txR.IsEnabled = false;
            await Task.Delay(100);
            txR.IsEnabled = true;

            if (!float.TryParse(txR.Text, out float resposta))
            {
                await DisplayAlert("Erro", "Digite um número válido!", "OK");
                processandoResposta = false;
                btOK.IsEnabled = true;
                IniciarTimer();
                return;
            }

            await ProcessarResposta(resposta);
        }

        private async Task ProcessarResposta(float resposta)
        {
            double tempoGasto = (DateTime.Now - inicioQuestao).TotalSeconds;
            bool acertou = Math.Abs(resposta - respostaCerta) < 0.01f;

            if (acertou)
            {
                imgR.Source = "win.png";

                int pontosBase = maxNumber switch
                {
                    10 => 1,
                    50 => 2,
                    100 => 3,
                    _ => 1
                };

                int bonusVelocidade = tempoGasto <= 10 ? 2 : (tempoGasto <= 20 ? 1 : 0);
                pontuacao += pontosBase + bonusVelocidade;
            }
            else
            {
                imgR.Source = "loose.png";
            }

            Points.Text = $"Pontuação: {pontuacao}";

            await Task.Delay(2000);

            if (rodadaAtual >= totalRodadas)
            {
                await MostrarResultadoFinal();
            }
            else
            {
                processandoResposta = false;
                GerarNovaQuestao();
            }
        }

        private async void TempoEsgotado()
        {
            if (processandoResposta) return;

            processandoResposta = true;
            imgR.Source = "loose.png";

            await Task.Delay(1500);

            if (rodadaAtual >= totalRodadas)
            {
                await MostrarResultadoFinal();
            }
            else
            {
                processandoResposta = false;
                GerarNovaQuestao();
            }
        }

        private async Task MostrarResultadoFinal()
        {
            string classificacao;
            int pontuacaoMaxima = totalRodadas * (maxNumber == 10 ? 3 : maxNumber == 50 ? 4 : 5);
            double percentual = (double)pontuacao / pontuacaoMaxima * 100;

            if (percentual >= 80) classificacao = "Excelente! 🏆";
            else if (percentual >= 60) classificacao = "Muito Bom! 👏";
            else if (percentual >= 40) classificacao = "Bom! 👍";
            else classificacao = "Continue Praticando! 💪";

            await DisplayAlert("Resultado Final",
                $"Sua pontuação: {pontuacao}\nClassificação: {classificacao}", "OK");

            ReiniciarJogo();
        }

        private void ReiniciarJogo()
        {
            timerToken?.Cancel();
            processandoResposta = false;
            pontuacao = 0;
            rodadaAtual = 0;

            Operations.SelectedIndex = -1;
            Points.Text = "Pontuação: 0";
            RoundLabel.Text = "0/10";
            TimerLabel.Text = "";
            TimerLabel.TextColor = Colors.Black;
            txR.Text = "";
            imgR.Source = "question.png";

            lb1.IsVisible = lb2.IsVisible = lb3.IsVisible = false;
            txR.IsVisible = btOK.IsVisible = imgR.IsVisible = false;
            Points.IsVisible = RoundLabel.IsVisible = TimerLabel.IsVisible = false;
        }
    }
}