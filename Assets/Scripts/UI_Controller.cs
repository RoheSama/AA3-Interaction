using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Controller : MonoBehaviour
{
    [Header("Strength Slider")]
    [SerializeField] private Slider _strengthSlider;
    private float _strengthSliderSpeed = 2f;
    private int _sliderDirection = 1;

    [Header("Effect Strength Slider")]
    [SerializeField] private Slider _effectStrengthSlider;
    private float _effectStrengthSliderSpeed = 2f;

    [Header("Angular Velocity Text")]
    [SerializeField] private TextMeshPro angularVelocityText;

    void Start()
    {
        _strengthSliderSpeed *= _strengthSlider.maxValue;

        _effectStrengthSliderSpeed *= _effectStrengthSlider.maxValue;
    }


    void Update()
    {
    }

    public void ResetStrengthSlider()
    {
        _strengthSlider.value = 0f;
        _sliderDirection = 1;
    }

    public void UpdateStrengthSlider()
    {
        _strengthSlider.value += Time.deltaTime * _strengthSliderSpeed * _sliderDirection;

        if (_strengthSlider.value <= _strengthSlider.minValue) _sliderDirection = 1;
        if (_strengthSlider.value >= _strengthSlider.maxValue) _sliderDirection = -1;
    }

    public float GetStrengthPer1()
    {
        return _strengthSlider.value / _strengthSlider.maxValue;
    }


    public void UpdateEffectStrengthSlider(int sliderDirection)
    {
        float newValue = _effectStrengthSlider.value + (Time.deltaTime * _effectStrengthSliderSpeed * sliderDirection);
        _effectStrengthSlider.value = Mathf.Clamp(newValue, 0f, _effectStrengthSlider.maxValue);
    }

    public float GetEffectStrengthPer1()
    {
        return _effectStrengthSlider.value / _effectStrengthSlider.maxValue;
    }

    public void SetAngularVelocityText(float degreesPerSecond)
    {
        angularVelocityText.text = degreesPerSecond.ToString();
    }

}
