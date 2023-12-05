using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class DiceRoller : MonoBehaviour
{
    #region editor

    //объекты
    [SerializeField] Transform edgeOrientations; //набор поворотов (rotations), при которых нужна грань смотрит в сторону камеры (повороты выставлены вручную в редакторе)
    [SerializeField] new Transform camera;
    [SerializeField] Animator diceAnimator;

    //UI
    [SerializeField] TextMeshProUGUI startHint;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] GameObject modifierContainer;

    //эффекты
    [SerializeField] GameObject particlesPrefab;

    #endregion

    static readonly int ANIMATION_ENLARGE = Animator.StringToHash("Enlarge");
    static readonly int ANIMATION_MOVE = Animator.StringToHash("MoveStart");
    static readonly int ANIMATION_START_ROTATE = Animator.StringToHash("RotationStart");
    static readonly int ANIMATION_END_ROTATE = Animator.StringToHash("RotationEnd");

    private Color SUCCESS_COLOR => new Color32(124, 255, 0, 0);
    private Color FAIL_COLOR => new Color32(255, 52, 30, 0);

    private DiceMoveAnimationBehaviour _moveBehaviour;
    private bool _diceClickable; //активен ли дайс для клика?

    private int _DC;
    private int _modifier;

    #region public

    public void SetDC(int DC)
        => _DC = DC;

    public void SetModifier(int modifier)
        => _modifier = modifier;

    #endregion

    #region UnityEvents

    void Awake()
    {
        _moveBehaviour = diceAnimator.GetBehaviour<DiceMoveAnimationBehaviour>();

        Reset();

        //по задумке эти значения должны приходить извне, для тестовой версии ставятся вручную
        SetDC(10);
        SetModifier(1);
    }

    void OnEnable()
    {
        //вызывается, когда заканчивается анимация броска дайса
        _moveBehaviour.OnMoveEnd += OnMoveEnd;
    }

    void OnDisable()
    {
        if (_moveBehaviour != null)
            _moveBehaviour.OnMoveEnd -= OnMoveEnd;
    }

    void Start()
    {
        StartCoroutine(Welcome());       
    }

    #endregion

    //начало игры - активируем подсказку и увеличиваем дайс
    private IEnumerator Welcome()
    {
        yield return new WaitForSeconds(0.5f);

        startHint.gameObject.SetActive(true);
        HideText(startHint);
        ShowText(startHint, 0.3f);

        _diceClickable = true;

        yield return new WaitForSeconds(0.5f);

        diceAnimator.SetTrigger(ANIMATION_ENLARGE);
    }

    //клик по дайсу
    private void OnMouseDown()
    {
        if (!_diceClickable)
            return;

        startHint.gameObject.SetActive(false);
        _diceClickable = false;

        diceAnimator.SetTrigger(ANIMATION_MOVE);
        diceAnimator.SetTrigger(ANIMATION_START_ROTATE);
    }

    //обработка окончания анимации броска дайса - вызывается через Animation State Machine
    private void OnMoveEnd(object sender, EventArgs e)
    {
        diceAnimator.ResetTrigger(ANIMATION_ENLARGE);
        diceAnimator.SetTrigger(ANIMATION_END_ROTATE);
        diceAnimator.applyRootMotion = false;

        //генерация значения, которое выпало
        var diceResult = UnityEngine.Random.Range(1, 21);
        SetTopEdge(diceResult);

        StartCoroutine(ShowResult(diceResult));
    }

    //прибавление модификатора к результату броска, показ итогового результата проверки
    private IEnumerator ShowResult(int result)
    {
        yield return new WaitForSeconds(0.3f);

        if (result == 20) //крит успех - модификатор неважен
        {
            diceAnimator.SetTrigger(ANIMATION_ENLARGE);

            yield return new WaitForSeconds(0.3f);

            resultText.color = SUCCESS_COLOR;
            resultText.text = "critical success";
            ShowText(resultText, 0.5f);
        }
        else if (result == 1) //крит провал - модификатор неважен
        {
            diceAnimator.SetTrigger(ANIMATION_ENLARGE);

            yield return new WaitForSeconds(0.3f);

            resultText.color = FAIL_COLOR;
            resultText.text = "critical failure";
            ShowText(resultText, 0.5f);
        }
        else
        {
            modifierContainer.LeanScale(Vector3.one * 1.35f, 0.3f)
                .setEaseOutQuad()
                .setOnComplete(
                    () => modifierContainer
                            .LeanScale(Vector3.one * 1f, 0.3f)
                            .setEaseOutQuad()
                );

            yield return new WaitForSeconds(0.7f);

            //прибавление модификатора - если сумма больше 20, то в ставим 20 (ограничение данной версии)
            var modifiedValue = Mathf.Min(result + _modifier, 20);
            SetTopEdge(modifiedValue);

            yield return new WaitForSeconds(0.4f);

            diceAnimator.SetTrigger(ANIMATION_ENLARGE);

            yield return new WaitForSeconds(0.3f);

            if (modifiedValue >= _DC) //успех
            {
                resultText.color = SUCCESS_COLOR;
                resultText.text = "success";
                ShowText(resultText, 0.5f);
            }
            else //провал
            {
                resultText.color = FAIL_COLOR;
                resultText.text = "failure";
                ShowText(resultText, 0.5f);
            }
        }

        yield return new WaitForSeconds(2f);

        //после таймаута возвращаем программу в исходное состояние для нового броска
        Reset();
        StartCoroutine(Welcome());
    }

    //поворот дайса к камере нужной гранью
    private void SetTopEdge(int value)
    {
        var orientation = edgeOrientations.GetChild(--value);

        var rotUp = Quaternion.FromToRotation(orientation.up, -camera.forward);
        transform.rotation = rotUp * transform.rotation;

        var rotFwd = Quaternion.FromToRotation(orientation.forward, camera.up);
        transform.rotation = rotFwd * transform.rotation;
    }

    //сброс к состоянию, которое предшествует броску
    private void Reset()
    {
        SetTopEdge(20);
        startHint.gameObject.SetActive(false);
        HideText(resultText);
        _diceClickable = false;
    }

    //эффект частиц при полете дайса - вызывается через Animation Event
    public void DiceCollideEffect()
    {
        var effect = Instantiate(particlesPrefab, transform.position, Quaternion.identity);
        Destroy(effect, 1f);
    }

    //выставляем альфа канал у цвета текста в 0
    private void HideText(TextMeshProUGUI text)
    {
        var color = new Color(text.color.r, text.color.g, text.color.b, 0f);
        text.color = color;
    }

    //плавно увеличиваем альфа канал у цвета текста до 1
    private void ShowText(TextMeshProUGUI textMesh, float time)
    {
        var _color = textMesh.color;
        LeanTween
            .value(textMesh.gameObject, _color.a, 1f, time)
            .setOnUpdate((float _value) => {
                _color.a = _value;
                textMesh.color = _color;
            });
    }
}
