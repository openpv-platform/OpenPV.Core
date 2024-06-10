#ifndef GAUGE_H
#define GAUGE_H

#include <QWidget>

namespace Ui {
class Gauge;
}

class Gauge : public QWidget
{
    Q_OBJECT

public:
    explicit Gauge(QWidget *parent = nullptr);
    ~Gauge();

private:
    Ui::Gauge *ui;
    const int start = 203*16;   //Arc start angle
    int span = 0;               //Arc Span Angle
    int max = 8;                //Maximum RPM value (x1000)
    int end = -3600;            //Largest span angle
    bool fw = false;            //Forwards/Backwards flag

    int GetDispVal(int span);   //Get Display value from given 'span'

protected:
    void GaugeEvent();          //Event for redrawing the gauge
};

#endif // GAUGE_H
