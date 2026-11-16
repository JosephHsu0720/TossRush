using TMPro;
using UnityEngine;

namespace TossRush
{
    public sealed class GameHUD : MonoBehaviour
    {
        public GameObject menuPanel, resultPanel, playPanel;
        public UnityEngine.UI.Button startButton, restartButton;
        public TMP_Text timerText, scoreText, stageText, hintText, feedbackText, resultText, bestText;
        public TMP_Text highScoreText;
        public CleanShotEffect cleanShotEffect;
        public WindIndicator windIndicator;
        public CollisionPointFeedback collisionFeedback;
        public UnityEngine.UI.Image timerFill, transition;
        public CanvasGroup feedback;
        public Material feedbackMaterial;
        Material materialInstance;
        float feedbackEnd;
        GameDirector game;

        public void Initialize(GameDirector director)
        {
            game = director;
            startButton.onClick.AddListener(game.StartRound);
            restartButton.onClick.AddListener(game.StartRound);
            materialInstance = new Material(feedbackMaterial);
            timerFill.material = materialInstance;
        }

        public void ShowMenu(int best)
        {
            collisionFeedback.Clear();
            cleanShotEffect.ResetEffect();
            cleanShotEffect.gameObject.SetActive(false);
            menuPanel.SetActive(true); resultPanel.SetActive(false); playPanel.SetActive(false);
            bestText.text = "HIGH SCORE  /  " + best.ToString("0000");
            SetTransition(0);
        }

        public void ShowPlay()
        {
            collisionFeedback.Clear();
            menuPanel.SetActive(false); resultPanel.SetActive(false); playPanel.SetActive(true);
            feedbackEnd = 0; feedback.alpha = 0;
            cleanShotEffect.gameObject.SetActive(true);
            cleanShotEffect.ResetEffect();
            Refresh(game.roundDuration, 0, game.ActiveStage.data, false);
        }

        public void Refresh(float time, int score, StageData data, bool released)
        {
            timerText.text = time.ToString("00.0") + "s";
            scoreText.text = score.ToString("0000");
            Vector3 wind = game.ActiveStage.CurrentWind;
            stageText.text = data.title.ToUpperInvariant() + "   /   +" + data.points;
            if (windIndicator) windIndicator.SetWind(wind, game.thrower.view.transform, data.accent);
            stageText.color = data.accent;
            string instruction = data.instruction;
            if (game.ActiveStage.Dartboard) instruction = game.ActiveStage.Dartboard.Instruction;
            if (data.verticalMotionAmplitude > 0)
                instruction = game.ActiveStage.CurrentVerticalAmplitude > 0 ? "BOTTLE FLIP / LEFT+RIGHT + UP+DOWN / PRIZES +10"
                    : "BOTTLE FLIP / LEFT+RIGHT / PRIZES +10 / CLEAR TO UNLOCK UP+DOWN";
            hintText.text = released ? "CLICK TO CAPTURE MOUSE  /  TIMER CONTINUES" : instruction + "   |   ESC : RELEASE MOUSE";
            timerFill.fillAmount = time/game.roundDuration;
            timerFill.color = time <= 5 ? new Color(1,0.3f,0.25f) : data.accent;
            bool newRecord = score > game.Best;
            highScoreText.text = (newRecord ? "NEW HIGH SCORE " : "HIGH SCORE ") + Mathf.Max(game.Best, score).ToString("0000");
            highScoreText.color = newRecord ? new Color(1, 0.78f, 0.34f) : data.accent;
            materialInstance.SetFloat("_Pulse", time <= 5 ? 0.65f : 0.08f);
            feedback.alpha = Mathf.Clamp01((feedbackEnd-Time.time)*3);
        }

        public void ShowCleanShot(int bonus) => cleanShotEffect.Play(bonus);
        public void ShowCollision(int points, Vector3 point, Camera view, Color color) => collisionFeedback.Show(points, point, view, color);
        public void ShowPenalty(int points, Vector3 point, Camera view) => collisionFeedback.Show(-points, point, view, new Color(1, .18f, .12f), false);

        public void ShowFeedback(string text, Color color)
        {
            feedbackText.text = text;
            feedbackText.color = color;
            feedbackEnd = Time.time+0.85f;
        }

        public void SetTransition(float alpha) => transition.color = new Color(0.025f,0.045f,0.08f,alpha);

        public void ShowResult(int score, int best, int hits, int shots, bool newRecord)
        {
            collisionFeedback.Clear();
            cleanShotEffect.ResetEffect();
            cleanShotEffect.gameObject.SetActive(false);
            playPanel.SetActive(false); menuPanel.SetActive(false); resultPanel.SetActive(true);
            resultText.text = "<size=20>YOUR SCORE</size>\n<size=78>"+score.ToString("0000")+"</size>\n"+
                "<size=18>HIGH SCORE  "+best.ToString("0000")+"   /   "+hits+" HITS   /   "+shots+" THROWS</size>"+
                (newRecord ? "\n<size=16><color=#FFC857>NEW HIGH SCORE!</color></size>" : "");
        }

        void OnDestroy() { if (materialInstance) Destroy(materialInstance); }
    }
}
