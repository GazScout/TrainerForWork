function toggleHint(id) {
    const el = document.getElementById('hint-' + id);
    el.style.display = el.style.display === 'none' ? 'block' : 'none';
}

function selectPhotoOption(taskId, btn, selected, correctAnswer) {
    const allBtns = document.querySelectorAll('#photo-options-' + taskId + ' .option-btn');
    const resultDiv = document.getElementById('photo-result-' + taskId);
    const explanationDiv = document.getElementById('explanation-' + taskId);

    allBtns.forEach(b => {
        b.disabled = true;
        b.classList.remove('selected', 'correct', 'wrong');
    });

    btn.classList.add('selected');

    if (selected.trim() === correctAnswer.trim()) {
        btn.classList.add('correct');
        resultDiv.innerHTML = '<span class="text-success fw-bold fs-5">✅ Правильно!</span>';
        resultDiv.className = 'mt-3 alert alert-success';
    } else {
        btn.classList.add('wrong');
        allBtns.forEach(b => {
            if (b.textContent.trim() === correctAnswer.trim()) b.classList.add('correct');
        });
        resultDiv.innerHTML = '<span class="text-danger fw-bold">❌ Неправильно.</span> Правильный ответ выделен зелёным.';
        resultDiv.className = 'mt-3 alert alert-danger';
    }

    resultDiv.style.display = 'block';
    if (explanationDiv) explanationDiv.style.display = 'block';
}

function checkForm(taskId) {
    const fields = document.querySelectorAll('#form-' + taskId + ' .form-input');
    const resultDiv = document.getElementById('form-result-' + taskId);
    const explanationDiv = document.getElementById('explanation-' + taskId);
    let correct = 0;
    let total = fields.length;

    fields.forEach(f => {
        const userVal = f.value.trim();
        const correctVal = f.dataset.correct;
        f.classList.remove('correct-input', 'wrong-input');
        if (userVal.length === 0) return;
        if (userVal.toLowerCase().includes(correctVal.toLowerCase()) || correctVal.toLowerCase().includes(userVal.toLowerCase())) {
            f.classList.add('correct-input');
            correct++;
        } else {
            f.classList.add('wrong-input');
            f.title = 'Ожидалось: ' + correctVal;
        }
    });

    const pct = Math.round((correct / total) * 100);
    resultDiv.innerHTML = `<strong>Результат:</strong> ${correct} из ${total} полей совпадают (${pct}%).<br>
                           <small class="text-muted">Проверка по частичному совпадению.</small>`;
    resultDiv.className = 'mt-3 alert ' + (pct >= 70 ? 'alert-success' : 'alert-warning');
    resultDiv.style.display = 'block';
    if (explanationDiv) explanationDiv.style.display = 'block';
}