#ifndef MINIGAUGE_H
#define MINIGAUGE_H

#include <QWidget>

QT_BEGIN_NAMESPACE
namespace Ui { class MiniGauge; }
QT_END_NAMESPACE

class MiniGauge : public QWidget
{
    Q_OBJECT

public:
    MiniGauge(QWidget *parent = nullptr);
    ~MiniGauge();

private:
    Ui::MiniGauge *ui;
    const int start = 2795;
    int span = 0;
    int end = -2710;
    bool fw = false;

    int GetDispVal(int span);

protected:
    void gaugeEvent();
};
#endif // MINIGAUGE_H
