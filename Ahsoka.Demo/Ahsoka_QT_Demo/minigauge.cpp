#include "minigauge.h"
#include "ui_minigauge.h"

#include <QPaintEvent>
#include <QPainter>
#include <QPen>
#include <QRectF>
#include <QTime>
#include <QTimer>

MiniGauge::MiniGauge(QWidget *parent)
    //MiniGauge widget set-up
    : QWidget(parent)
    , ui(new Ui::MiniGauge)
{
    ui->setupUi(this);

    //Create a timer and connect it to the Gauge Event in
    //order to update the gauge at a predefined interval.
    QTimer *timer = new QTimer(this);
    connect(timer, &QTimer::timeout, this, &MiniGauge::gaugeEvent);
    timer->start(50);
}


MiniGauge::~MiniGauge()
{
    delete ui;
}

int MiniGauge::GetDispVal(int span)
{
    //Simple slope calculation to map span to display value
    int output_start = 0, output_end = 100, input_start = 100, input_end = end;

    double slope = 1.0 * (output_end - output_start) / (input_end - input_start);
    int output = output_start + slope * (span - input_start);

    return output;
}

void MiniGauge::gaugeEvent()
{
    //Create a blank transparent pixmap with the same size as
    //the ArcSpace pixmap
    QPixmap pm(this->ui->ArcSpace->size());
    pm.fill(Qt::transparent);

    //Draw a somewhat translucent grey arc on the new pixmap
    QPainter painter(&pm);
    QColor ArcBrush(Qt::gray);
    QPen pen(ArcBrush, 10);
    painter.setPen(pen);
    painter.setOpacity(0.7);
    QRectF rectangle(5, 5, 110, 110);
    painter.drawArc(rectangle, start, span);

    //Replace the ArcSpace pixmap with the new one
    if(span != 0)
        this->ui->ArcSpace->setPixmap(pm);

    //Update the Numeric Indicator
    ui->MG_PSI_Indicator->setNum(GetDispVal(span));

    //Update span for the next frame
    if (span <= end || span >= 0)
        fw = !fw;
    int inc = 32;
    if (fw)
        span-=inc;
    else
        span+=inc;
}
