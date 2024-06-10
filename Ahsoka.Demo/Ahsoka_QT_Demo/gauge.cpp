#include "Gauge.h"
#include "ui_gauge.h"

#include <QPaintEvent>
#include <QPainter>
#include <QPen>
#include <QRectF>
#include <QTime>
#include <QTimer>

Gauge::Gauge(QWidget *parent) :
    //Gauge widget set-up
    QWidget(parent),
    ui(new Ui::Gauge)
{
    ui->setupUi(this);

    //Create a timer and connect it to the Gauge Event in
    //order to update the gauge at a predefined interval.
    QTimer *timer = new QTimer(this);
    connect(timer, &QTimer::timeout, this, &Gauge::GaugeEvent);
    timer->start(33);
}

Gauge::~Gauge()
{
    delete ui;
}

int Gauge::GetDispVal(int span)
{
    //Really simple range checker. Checks the 'span' value
    //and returns the appropriate display value
    int DispVal = 0;
    if (span < -245 && span >= -725)
    {
        DispVal = 1;
    }
    else if (span < -725 && span >= -1210)
    {
        DispVal = 2;
    }
    else if (span < -1210 && span >= -1695)
    {
        DispVal = 3;
    }
    else if (span < -1695 && span >= -2180)
    {
        DispVal = 4;
    }
    else if (span < -2180 && span >= -2665)
    {
        DispVal = 5;
    }
    else if (span < -2665 && span >= -3150)
    {
        DispVal = 6;
    }
    else if (span < -3150 && span >= -3600)
    {
        DispVal = 7;
    }
    else if (span < -3600)
    {
        DispVal = 8;
    }
    else {}

    return DispVal;
}

void Gauge::GaugeEvent()
{
    //Create a blank transparent pixmap with the same size as
    //the ArcSpace pixmap
    QPixmap pm(this->ui->ArcSpace->size());
    pm.fill(Qt::transparent);

    //Draw a yellow arc on the new pixmap
    QPainter painter(&pm);
    QPen pen(Qt::yellow, 38);
    painter.setPen(pen);
    QRectF rectangle(33.0, 33.0, 315, 315);
    painter.drawArc(rectangle, start, span);

    //Replace the ArcSpace pixmap with the new one
    if(span != 0)
        this->ui->ArcSpace->setPixmap(pm);

    //Update the Numeric Indicator
    ui->RPMIndicator->setNum(GetDispVal(span));

    //Update 'span' for the next frame
    if (span <= end || span >= 0)
        fw = !fw;
    int inc = 32;
    if (fw)
        span-=inc;
    else
        span+=inc;
}
